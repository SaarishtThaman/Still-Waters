mergeInto(LibraryManager.library, {
  SW_Vibrate: function (pattern) {
    try {
      if (!navigator.vibrate) return;
      var s = UTF8ToString(pattern);
      navigator.vibrate(s.split(',').map(Number));
    } catch (e) {}
  }
});
