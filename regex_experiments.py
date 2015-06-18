import re

link_regex = re.compile("\[(?P<text>(\w?\s?)+)(\|(?P<link>(\w?\s?)+))?\]", re.UNICODE)

def url_for(method, entry):
    return '/haslo/' + str(entry)

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

    
    
print subst_links("Tak [drinkowalem|drinkowac] ze wyrzucila mnie do [bejsmentu|bejsment]")
print subst_links("Lubie [drinkowac]")
print subst_links("Tak")
