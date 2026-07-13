mergeInto(LibraryManager.library, {
    GetUsedJSHeapSizeInternal: function () {
        try {
            if (typeof wx !== 'undefined' && wx.getPerformance && wx.getPerformance().memory) {
                return wx.getPerformance().memory.usedJSHeapSize || 0;
            }
            if (typeof performance !== 'undefined' && performance.memory) {
                return performance.memory.usedJSHeapSize || 0;
            }
        } catch (e) {
            console.warn('[MPSWeChatBridge] GetUsedJSHeapSize failed:', e);
        }
        return -1;
    },

    GetPerformanceTimestampInternal: function () {
        try {
            if (typeof wx !== 'undefined' && wx.getPerformance && wx.getPerformance().now) {
                return wx.getPerformance().now();
            }
            if (typeof performance !== 'undefined' && performance.now) {
                return performance.now();
            }
        } catch (e) {
            console.warn('[MPSWeChatBridge] GetPerformanceTimestamp failed:', e);
        }
        return -1;
    }
});
