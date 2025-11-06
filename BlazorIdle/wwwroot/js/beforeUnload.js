// BeforeUnload handler for BlazorIdle
// Handles saving character data before page closes

window.blazorIdle = window.blazorIdle || {};

(function() {
    let dotNetHelper = null;
    let isRegistered = false;
    let saveInProgress = false;

    // Synchronous save function that uses sendBeacon for reliability
    function triggerSave() {
        if (!dotNetHelper || saveInProgress) {
            return;
        }
        
        saveInProgress = true;
        console.log('Triggering save before page unload');
        
        try {
            // Invoke the .NET method - don't await, just fire and forget
            // The browser will keep the page alive briefly for sendBeacon/fetch with keepalive
            dotNetHelper.invokeMethodAsync('OnBeforeUnload')
                .then(() => {
                    console.log('Save triggered successfully');
                    saveInProgress = false;
                })
                .catch(error => {
                    console.error('Error triggering save:', error);
                    saveInProgress = false;
                });
        } catch (error) {
            console.error('Error invoking save method:', error);
            saveInProgress = false;
        }
    }

    // Handler function for beforeunload event
    function handleBeforeUnload(event) {
        console.log('beforeunload event fired');
        triggerSave();
        // Don't prevent default or show dialog
    }
    
    // Handler for pagehide event (more reliable for mobile/modern browsers)
    function handlePageHide(event) {
        console.log('pagehide event fired');
        triggerSave();
    }
    
    // Handler for visibilitychange event
    function handleVisibilityChange() {
        if (document.visibilityState === 'hidden') {
            console.log('visibilitychange to hidden - page may be closing');
            triggerSave();
        }
    }

    // Register beforeunload event
    window.blazorIdle.registerBeforeUnload = function(dotNetRef) {
        if (isRegistered) {
            console.log('BeforeUnload already registered');
            return;
        }

        dotNetHelper = dotNetRef;
        
        // Register multiple events for better compatibility
        window.addEventListener('beforeunload', handleBeforeUnload);
        window.addEventListener('pagehide', handlePageHide);
        window.addEventListener('visibilitychange', handleVisibilityChange);
        
        isRegistered = true;
        console.log('Page unload handlers registered (beforeunload, pagehide, visibilitychange)');
    };

    // Unregister beforeunload event
    window.blazorIdle.unregisterBeforeUnload = function() {
        if (dotNetHelper) {
            window.removeEventListener('beforeunload', handleBeforeUnload);
            window.removeEventListener('pagehide', handlePageHide);
            window.removeEventListener('visibilitychange', handleVisibilityChange);
            dotNetHelper = null;
            isRegistered = false;
            console.log('Page unload handlers unregistered');
        }
    };
})();
