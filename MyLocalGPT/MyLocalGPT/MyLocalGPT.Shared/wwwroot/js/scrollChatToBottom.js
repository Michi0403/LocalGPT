window.scrollChatToBottom = function (elementId)
{
    const el = document.getElementById(elementId);
    //if (el) {
    //    el.scrollTop = el.scrollHeight;
    //}
    if (el) {
        setTimeout(() => {
            var bottomElement = el.lastElementChild;
            bottomElement.scrollIntoView({ behavior: 'smooth', block: 'end' });
            el.scrollTop = el.scrollHeight;
        }, 150); // Give layout time to finish
    }
}