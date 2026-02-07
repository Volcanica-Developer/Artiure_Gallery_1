mergeInto(LibraryManager.library, {
  GetBrowserUrl: function () {
    var url = window.location.href;
    var bufferSize = lengthBytesUTF8(url) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(url, buffer, bufferSize);
    return buffer;
  },

  GetExhibitionIdFromUrl: function () {
    var pathname = window.location.pathname;
    // Match /exhibition/{uuid} pattern
    var match = pathname.match(/\/exhibition\/([a-zA-Z0-9-]+)/);
    var exhibitionId = match ? match[1] : "";
    
    var bufferSize = lengthBytesUTF8(exhibitionId) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(exhibitionId, buffer, bufferSize);
    return buffer;
  }
});
