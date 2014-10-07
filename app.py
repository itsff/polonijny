import os
import json
import re
from random import randint
from flask import Flask
from flask import Markup
from flask import render_template
from flask import abort, redirect, url_for

from pymongo import MongoClient
from pymongo import ASCENDING, DESCENDING

app = Flask(__name__)

MONGO_URL = os.environ["MONGOLAB_URI"]

mongoClient = MongoClient(MONGO_URL)
db = mongoClient.get_default_database()
entries = db.entries

link_regex = re.compile("\[(?P<text>(\w?\s?)+)(\|(?P<link>(\w?\s?)+))?\]", re.UNICODE)


def get_letters():
    return entries.distinct('letter')


def get_random_entry():
    total = entries.count()
    for c in entries.find().limit(1).skip(randint(0, total)):
        return c


def massage_entry_examples(entry):
    examples_html = []

    if "examples" in entry:
        for ex in entry["examples"]:
            m = link_regex.search(ex)
            if m is not None:
                g = m.groupdict()
                link = url_for('show_entry', entry=g["text"])
                if g["link"] is not None:
                    link = g["link"]

                atag = u'<a href="%s">%s</a>' % (link, g["text"])

                ex_html = link_regex.sub(atag, ex, re.UNICODE)
                examples_html.append(ex_html)

            else:
                examples_html.append(ex)

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

@app.route('/losuj')
def losuj():
    entry = get_random_entry()
    return redirect(url_for('show_entry', entry=entry["entry"]))

@app.route('/litera/<letter>')
def show_letter(letter):
    e = []
    cursor = entries.find({'letter': letter.lower()}).sort('entry', 1)
    for d in cursor:
        e.append(d)

    return render_template(
        "letter.html",
        entries=e,
        current_letter=letter,
        letters=get_letters())


@app.route('/haslo/<entry>')
def show_entry(entry):
    found = []
    for f in entries.find({'entry': entry}):
        massage_entry_examples(f)
        found.append(f)

    return render_template(
        "entry.html",
        entries=found,
        letters=get_letters())


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

##############################################################

if __name__ == '__main__':
    app.run(debug=True)
