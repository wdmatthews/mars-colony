mergeInto(LibraryManager.library, {
  Load: function () {
    load();
  },
  Save: function(saveData) {
    save(Pointer_stringify(saveData));
  },
});