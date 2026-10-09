mergeInto(LibraryManager.library, {
    GetGistId: function () {
        var searchString = window.location.search;

        if (document.referrer) {
            try {
                var referrerUrl = new URL(document.referrer);
                if (referrerUrl.searchParams.has('gist')) {
                    searchString = referrerUrl.search;
                }
            } catch (e) {
                console.error("Fout bij uitlezen referrer:", e);
            }
        }

        var urlParams = new URLSearchParams(searchString);
        var gistId = urlParams.get('gist');

        if (!gistId) {
            return 0;
        }

        var bufferSize = lengthBytesUTF8(gistId) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(gistId, buffer, bufferSize);
        return buffer;
    }
});