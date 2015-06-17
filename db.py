import os
import json
from pymongo import MongoClient
from bson.objectid import ObjectId

#stream = file('static/slownik.json', 'r')
#entries = json.load(stream)

#for e in entries:
#	e['letter'] = e['entry'][0].lower()
	
client = MongoClient(os.environ['MONGOLAB_URI'])
#client = MongoClient('mongodb://admin:polska12345678@ds027829.mongolab.com:27829/heroku_app21400621')

db = client.get_default_database()
col = db.entries

e = []
for x in col.find({}):
        e.append(x)


for n in e:
        print n[u'entry']       
        n[u'entry_lower_case'] = n[u'entry'].lower()
        col.replace_one({'_id': n[u'_id']}, n)
        
