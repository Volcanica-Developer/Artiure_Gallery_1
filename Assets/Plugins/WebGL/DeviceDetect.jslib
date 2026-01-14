mergeInto(LibraryManager.library, {
  IsMobileBrowser: function () {
    var ua = navigator.userAgent || navigator.vendor || window.opera || "";
    ua = ua.toLowerCase();

    // Basic mobile browser detection (phones + tablets)
    var isMobile = /android|iphone|ipod|ipad|iemobile|blackberry|opera mini|mobile/.test(ua);
    return isMobile ? 1 : 0;
  }
});
