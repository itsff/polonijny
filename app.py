# coding: utf-8

import os
import json
import re
import validate
import datetime
import locale

from recaptcha import ReCaptcha
from random import randint
from flask import Flask
from flask import Markup
from flask import render_template, request
from flask import abort, redirect, url_for, send_from_directory

from pymongo import MongoClient
from pymongo import ASCENDING, DESCENDING

import urllib3
urllib3.disable_warnings()

locale.setlocale(locale.LC_ALL, 'pl_PL.UTF-8')

app = Flask(__name__, static_folder='static', static_url_path='')

MONGO_URL = os.environ["MONGOLAB_URI"]

recaptcha = ReCaptcha(app, "6LcpgQcTAAAAAFqGZbDtRKcBp7WIDi_UFARU1ihk", "6LcpgQcTAAAAADh_6O8r9G_8KwnyNnYh7X4F0YXP")
mongoClient = MongoClient(MONGO_URL)
db = mongoClient.get_default_database()
entries = db.entries

link_regex = re.compile("\[(?P<text>(\w?\s?)+)(\|(?P<link>(\w?\s?)+))?\]", re.UNICODE)


def get_letters():
    l = list(entries.distinct('letter'))
    l.sort(locale.strcoll)
    return l


def get_random_entry():
    total = entries.count()
    for c in entries.find().limit(1).skip(randint(0, total)):
        return c


def subst_links(string):
    result = string
    matches = link_regex.finditer(string)
    for m in matches:
        g = m.groupdict()
        link = url_for('show_entry', entry=g["text"])
        if g["link"] is not None:
            link = url_for('show_entry', entry=g["link"])
            
        atag = u'<a href="%s">%s</a>' % (link, g["text"])
        result = link_regex.sub(atag, result, count=1)

    return result

    
def massage_entry_examples(entry):
    examples_html = []

    if "examples" in entry:
        for ex in entry["examples"]:
            examples_html.append(subst_links(ex))

        entry["examples_html"] = examples_html


@app.route('/')
def home():
    return render_template(
        "home.html",
        letters=get_letters(),
        entry=get_random_entry())


@app.route('/about')
def about():
    return render_template("about.html",
                           letters=get_letters())

    
@app.route('/dodaj', methods=['POST', 'GET'])
def dodaj():
    if request.method == 'POST':
        (problems, obj) = validate.validateAddedEntry(request.data)
        if problems != None:
            return problems, 400

        if not recaptcha.verify(response=obj['g-recaptcha-response']):
            return "Jesteś robotem?", 401    

        del obj['g-recaptcha-response']
        obj['from_internet'] = True
        obj['ip'] = str(request.remote_addr)
        obj['utc_stamp'] = str(datetime.datetime.utcnow())

        # All good. Let's insert into DB
        entries.insert_one(obj)
        return json.dumps({'success':True}), 200, {'ContentType':'application/json'} 
    
    return render_template("add.html",
                           letters=get_letters())

@app.route('/losuj')
def losuj():
    entry = get_random_entry()
    return redirect(url_for('show_entry', entry=entry["entry"]))

@app.route('/nowe')
def nowe():
    e = []
    cursor = entries.find(sort=[('$natural', -1)], limit=25)
    for d in cursor:
        e.append(d)

    return render_template(
        "letter.html",
        entries=e,
        header_text="Najnowsze",
        letters=get_letters()) 

@app.route('/litera/<letter>')
def show_letter(letter):
    e = []
    cursor = entries.find({'letter': letter.lower()}).sort('entry_lower_case', 1)
    for d in cursor:
        e.append(d)

    return render_template(
        "letter.html",
        entries=e,
        header_text=letter.upper(),
        letters=get_letters())


@app.route('/haslo/<entry>')
def show_entry(entry):
    found = []
    for f in entries.find({'entry_lower_case': entry.lower()}):
        massage_entry_examples(f)
        found.append(f)

    if len(found) > 0:
        return render_template(
            "entry.html",
            entries=found,
            letters=get_letters())
    else:
        return render_template(
            "not_found.html",
            entry=entry,
            letters=get_letters())


@app.route('/hasla')
def get_all_entries():
    ents = entries.distinct('entry')
    return json.dumps(ents)

# #############################################################
# Short routes. We will handle these as redirects
# so that search engines will only have 1 URL scheme
# to deal with
@app.route('/l/<letter>')
def show_letter_short(letter):
    return redirect(url_for('show_letter', letter=letter))


@app.route('/h/<entry>')
def show_entry_short(entry):
    return redirect(url_for('show_entry', entry=entry))


@app.route('/robots.txt')
@app.route('/sitemap.xml')
def static_from_root():
    return send_from_directory(app.static_folder, request.path[1:])

##############################################################

if __name__ == '__main__':
    is_debug = os.environ["FLASK_DEBUG"] is not None
    app.run(debug=is_debug)
