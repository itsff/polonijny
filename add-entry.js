
class Entry
{
    constructor (str)
    {
        this.value = str;
    }
}


export class AddEntry {
    
    constructor() {
        this.entry = "";
        
        this.meanings = [];
        this.english  = [];
        this.examples = [];
        this.see_also = [];
    }
    
    normalize_col (col) {
        var result = [];
        for (var i=0,  tot=col.length; i < tot; ++i) {
            
            var s = col[i].value.trim();
            if (s.length > 0)
            {
                result.push(s);
            }
        }
        return result;
    }
    
    submit () {
        var payload = this.exportModel();
        var json_str = JSON.stringify(payload);
        console.log(json_str);
    }
    
    add (col) {
        col.push(new Entry(""));
    }
    
    remove (col, idx) {
        col.splice(idx, 1);
    }
    
    exportModel () {
        var payload = {
            'entry'    : this.entry.trim(),
            'meanings' : this.normalize_col(this.meanings),
            'english'  : this.normalize_col(this.english),
            'examples' : this.normalize_col(this.examples),
            'see_also' : this.normalize_col(this.see_also)
        };
        
        return payload;
    }
    
    get canSubmit () {
        var p = this.exportModel();
        
        return (p.entry.length > 0 && 
            (p.meanings.length > 0 || p.english.length > 0 || p.examples.length > 0 || p.see_also.length > 0));
    }
    
}
