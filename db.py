import os
import json
from pymongo import MongoClient


stream = file('static/slownik.json', 'r')
entries = json.load(stream)

for e in entries:
	e['letter'] = e['entry'][0].lower()
	
client = MongoClient(os.environ['MONGOLAB_URI'])

# 'mongodb://admin:polska12345678@ds027779.mongolab.com:27779/heroku_app21400621')

db = client.heroku_app21400621
col = db.entries

col.insert(entries)