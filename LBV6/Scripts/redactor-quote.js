$.Redactor.prototype.quote = function()
{
    return {
        getTemplate: function() {
            return String()
                + "<div class=\"modal-section\" id=\"redactor-modal-quote\">"
                    + "<section>"
                        + "<textarea id=\"quote-modal-quote\" placeholder=\"Quote...\" rows=\"4\" class=\"form-control\"></textarea>"
                        + "<div class=\"mt10\">"
                            + "<input type=\"text\" class=\"form-control\" placeholder=\"Credit (optional)...\" id=\"quote-modal-credit\">"
                        + "</div>"
                    + "</section>"
                    + "<section class=\"text-right\">"
                        + "<button id=\"redactor-modal-button-cancel\" class=\"btn btn-default mr10\">Cancel</button>"
                        + "<button id=\"redactor-modal-button-action\" class=\"btn btn-danger\">Add Quote</button>"
                    + "</section>"
                + "</div>";
        },
        init: function ()
        {
            var button = this.button.add("quote", "Quote");
            this.button.addCallback(button, this.quote.show);
        },
        show: function()
        {
            this.modal.addTemplate("quote", this.quote.getTemplate());
            this.modal.load("quote", "Quote Modal", 400);
 
            var button = this.modal.getActionButton();
            button.on("click", this.quote.insert);
 
            this.modal.show();
 
            $("#quote-modal-quote").focus();
        },
        insert: function()
        {
            var quote = $("#quote-modal-quote").val();
            var credit = $("#quote-modal-credit").val();

            if (window.IsNullOrEmpty(credit)) {
                quote = "<blockquote><p>"+quote+"</p></blockquote>";
            } else {
                quote = "<blockquote><p>"+quote+"</p><footer>"+credit+"</footer></blockquote>";
            }
 
            this.modal.close();
            this.insert.html(quote);
        }
    };
};