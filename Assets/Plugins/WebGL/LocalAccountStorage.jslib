mergeInto(LibraryManager.library, {
    TrickalFlushLocalStorage: function () {
        if (Module.trickalSyncRunning) { Module.trickalSyncAgain = true; return; }
        Module.trickalSyncRunning = true;
        var flush = function () {
            Module.trickalSyncAgain = false;
            FS.syncfs(false, function (error) {
                if (error) console.error("Local account/deck persistence failed", error);
                if (Module.trickalSyncAgain) flush();
                else Module.trickalSyncRunning = false;
            });
        };
        flush();
    }
});
