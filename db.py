import os
import json
from pymongo import MongoClient


stream = file('static/slownik.json', 'r')
entries = json.load(stream)

for e in entries:
	e['letter'] = e['entry'][0].lower()
	
client = MongoClient(os.environ['MONGOLAB_URI'])
#client = MongoClient('mongodb://admin:polska12345678@ds027829.mongolab.com:27829/heroku_app21400621')

db = client.get_default_database()
col = db.entries

col.insert(entries)
