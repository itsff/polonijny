import json


def validateCollection(obj, colName):
    finalCol = []
    
    if colName in obj:
        col = obj[colName]

        if isinstance(col, list):
            for e in col:
                e = e.strip()
                if len(e) > 2:
                    finalCol.append(e)
        
    return finalCol
    

def validateAddedEntry(content):

    finalObj = {}
    
    try:
        obj = json.loads(content)
    except:
        return ('Cos nie tak. Sprobuj jeszcze raz.', {})

    ### Entry
    if 'entry' in obj:
        entry = str(obj['entry']).strip()

        if len(entry) < 2:
            return ('Haslo jest wymagane', {})    
    else:
        return ('Haslo jest wymagane.', {})

    finalObj['entry']  = entry
    finalObj['letter'] = entry[0]

    finalObj['meanings']          = validateCollection(obj, 'meanings')
    finalObj['english_meanings']  = validateCollection(obj, 'english')
    finalObj['examples']          = validateCollection(obj, 'examples')
    finalObj['see_also']          = validateCollection(obj, 'see_also')

    if len(finalObj['meanings']) == 0 and len(finalObj['see_also']) == 0:
        return ("Musisz podac przynajmniej jedno znaczenie albo haslo pokrewne", {})

    return ("", finalObj)

if __name__ == "__main__":

    data = '''{
"entry": " rymot    ",
"meanings": ["", "Ha ha"],
"english": {}
}
    '''

    x = validateAddedEntry(data)
    print x
