window.getAllAsync = async function getAllAsync(typeName, additionalQuery = null) {
    let url = `api/odata/${typeName}`;
    if (additionalQuery) {
        url += `?${additionalQuery}`;
    }

    try {
        const response = await fetch(url, {
            method: 'GET',
            credentials: 'include', // send auth cookie
            headers: {
                'Accept': 'application/json, application/octet-stream',
                'Content-MessageType': 'application/json; odata.metadata=full'
            }
        });

        if (!response.ok) {
            console.warn(`Fetch failed: ${response.status}`);
            return [];
        }

        const data = await response.json();
        return data.value || [];
    } catch (err) {
        console.error('Fetch error:', err);
        return [];
    }
}

//window.startBlazorRefresh = function () {
//    setInterval(() => {
//        DotNet.invokeMethodAsync('TacosPortal', 'TriggerRefreshFromJs')
//            .catch(err => console.warn('Blazor refresh failed:', err));
//    }, 10000); // every 10 seconds
//};