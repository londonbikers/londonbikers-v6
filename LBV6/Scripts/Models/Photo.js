function PhotoModel(data) {

    var self = this;
    self.PostId = data.PostId;
    self.Id = data.Id;
    self.FilestoreId = data.FilestoreId;
    self.Created = data.Created;
    self.Credit = data.Credit;
    self.Caption = ko.observable(data.Caption);
    self.Position = ko.observable(data.Position);
    self.Width = data.Width;
    self.Height = data.Height;

    self.Comments = ko.observableArray();
    data.Comments.forEach(function(comment) {
        self.Comments.push(new PhotoCommentModel(comment));
    });

    /**
     * Gets the width of the photo after dpi scaling
     * i.e. if the photo is 2000 pixels wide and being displayed on HDPI screen with a 2x
     * scaling factor then the scaled width would be 1000 pixels.
     */
    self.GetScaledWidth = ko.pureComputed(function () {
        return self.Width / GetPixelRatio();
    }, self);

}