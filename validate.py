# coding: utf-8
import json


def validateCollection(obj, colName):
    finalCol = []
    
    if colName in obj:
        col = obj[colName]

        if isinstance(col, list):
            for e in col:
                e = unicode(e.strip())
                if len(e) > 2:
                    finalCol.append(e)
        
    return finalCol
    

def validateAddedEntry(content):

    finalObj = {}
    
    try:
        obj = json.loads(content)
    except:
        return (u'Coś nie tak. Spróbuj jeszcze raz.', {})

    if not 'g-recaptcha-response' in obj:
        return "Czy jesteś robotem?" 
        
    ### Entry
    if 'entry' in obj:
        entry = unicode(obj['entry'].strip())

        if len(entry) < 2:
            return (u'Hasło jest wymagane.', {})    
    else:
        return (u'Hasło jest wymagane.', {})

    finalObj['entry']  = unicode(entry)
    finalObj['entry_lower_case']  = unicode(entry).lower()
    finalObj['letter'] = unicode(entry[0])
    finalObj['g-recaptcha-response'] = obj['g-recaptcha-response']

    finalObj['meanings']          = validateCollection(obj, 'meanings')
    finalObj['english_meanings']  = validateCollection(obj, 'english')
    finalObj['examples']          = validateCollection(obj, 'examples')
    finalObj['see_also']          = validateCollection(obj, 'see_also')

    if len(finalObj['meanings']) == 0 and len(finalObj['see_also']) == 0:
        return (u"Musisz podać przynajmniej jedno znaczenie albo hasło pokrewne.", {})

    return (None, finalObj)

if __name__ == "__main__":

    data = '''{
"entry": " rymot    ",
"meanings": ["", "Ha ha"],
"english": {}
}
    '''

    x = validateAddedEntry(data)
    print x
