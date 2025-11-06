// BeforeUnload handler for BlazorIdle
// Handles saving character data before page closes

window.blazorIdle = window.blazorIdle || {};

(function() {
    let dotNetHelper = null;
    let isRegistered = false;

    // Handler function for beforeunload event
    async function handleBeforeUnload(event) {
        if (dotNetHelper) {
            try {
                // Call the .NET method to save character
                await dotNetHelper.invokeMethodAsync('OnBeforeUnload');
                console.log('Character save initiated before unload');
            } catch (error) {
                console.error('Error saving character before unload:', error);
            }
        }
    }

    // Register beforeunload event
    window.blazorIdle.registerBeforeUnload = function(dotNetRef) {
        if (isRegistered) {
            console.log('BeforeUnload already registered');
            return;
        }

        dotNetHelper = dotNetRef;
        window.addEventListener('beforeunload', handleBeforeUnload);
        isRegistered = true;
        console.log('BeforeUnload handler registered');
    };

    // Unregister beforeunload event
    window.blazorIdle.unregisterBeforeUnload = function() {
        if (dotNetHelper) {
            window.removeEventListener('beforeunload', handleBeforeUnload);
            dotNetHelper = null;
            isRegistered = false;
            console.log('BeforeUnload handler unregistered');
        }
    };
})();
