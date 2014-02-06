import os
import json
from flask import Flask
from flask import render_template
from flask import abort, redirect, url_for

from pymongo import MongoClient
from pymongo import ASCENDING, DESCENDING

app = Flask(__name__)

MONGO_URL   = os.environ["MONGOLAB_URI"]

mongoClient = MongoClient(MONGO_URL)
db          = mongoClient.get_default_database()
entries     = db.entries




@app.route('/')
def home():
    return render_template("home.html")

@app.route('/about')
def about():
	return 'ha ha ha ha ha'



@app.route('/litera/<letter>')
def show_letter(letter):
	e = []
	cursor = entries.find( { 'letter' : letter.lower() }).sort('entry', 1)
	for d in cursor:
		e.append(d)

	return str(e)

@app.route('/haslo/<entry>')
def show_entry(entry):

	found = []
	for f in entries.find( { 'entry' : entry }):
		found.append(f)

	return str(found)


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
