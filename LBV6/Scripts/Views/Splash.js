$(document).ready(function () {
    $.getJSON("/api/legacy/GetRandomPopularGalleryImage", function (url) {
        $.backstretch(url);
    });
});