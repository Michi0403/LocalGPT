window.loginToSetCookie = async function (apiRoute, userName, password) {
    console.log("Sending login for:", apiRoute, userName, password);

    const body = JSON.stringify({ UserName: userName, Password: password });
    console.log("Body:", body);

    const response = await fetch(apiRoute, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        credentials: "include",
        body: body
    });

    return response.ok;
}