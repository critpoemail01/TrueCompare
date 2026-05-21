const handlers = new Map();

export function connect(pageId, dotNetReference) {
    disconnect(pageId);

    const handler = (event) => {
        if (event.key !== "Enter" || event.defaultPrevented || event.isComposing) {
            return;
        }

        if (event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        const page = document.getElementById(pageId);
        if (!page) {
            return;
        }

        const activeElement = document.activeElement;
        if (activeElement && activeElement !== document.body && !page.contains(activeElement)) {
            return;
        }

        const tagName = activeElement?.tagName?.toLowerCase();
        const inputType = activeElement?.getAttribute("type")?.toLowerCase();
        if (tagName === "textarea" || (tagName === "input" && inputType !== "range")) {
            return;
        }

        if (tagName === "a") {
            return;
        }

        event.preventDefault();
        dotNetReference.invokeMethodAsync("SubmitCriteriaFromEnter");
    };

    document.addEventListener("keydown", handler, true);
    handlers.set(pageId, handler);
}

export function disconnect(pageId) {
    const handler = handlers.get(pageId);
    if (!handler) {
        return;
    }

    document.removeEventListener("keydown", handler, true);
    handlers.delete(pageId);
}
