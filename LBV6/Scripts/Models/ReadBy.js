function ReadByModel(data) {
    var self = this;

    if (data != undefined) {
        self.Id = data.Id;
        self.UserId = data.UserId;
        self.UserName = data.UserName;
        self.When = data.When;
    } else {
        self.Id = null;
        self.UserId = null;
        self.UserName = null;
        self.When = null;
    }
}