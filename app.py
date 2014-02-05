import os
import json
from flask import Flask
from flask import render_template
from flask import abort, redirect, url_for

from pymongo import MongoClient
from pymongo import ASCENDING, DESCENDING

app = Flask(__name__)






@app.route('/populate')
def populate():
	stream = file('static/slownik.json', 'r')
	
	entries = json.load(stream)


	mongoClient = MongoClient(os.environ["MONGOLAB_URI"])
	db = mongoClient.slownik

	col = db.entries

	col.insert(entries)
	
	return str(entries)


@app.route('/')
def home():
    return render_template("home.html")

@app.route('/about')
def about():
	return 'ha ha ha ha ha'




@app.route('/litera/<letter>')
def show_letter(letter):
	return 'you selected letter: ' + letter

@app.route('/haslo/<entry>')
def show_entry(entry):
	return 'looking for %s?' % entry


##############################################################
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
