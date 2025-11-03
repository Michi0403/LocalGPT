window.scrollWindowToBottom = function ()
{
    setTimeout(() => {
        window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' });
    }, 150);
}