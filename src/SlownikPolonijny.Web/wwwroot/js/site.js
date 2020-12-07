
let searchOptions = {
    url: function(phrase) { return "/szukaj/" + phrase; },
    requestDelay: 500,
    list: {
		onChooseEvent: function() {
                  let name = $("#search").getSelectedItemData();
                  window.location.href = "/haslo/" + name;
            }	
	}
};

$("#search").easyAutocomplete(searchOptions);