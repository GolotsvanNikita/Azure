window.translationTask = 0;

document.addEventListener
(
    'selectionchange', () =>
    {
        const target = document.getSelection().toString();
        console.log(target);
        console.log(document.getSelection().toString());
        if (window.translationTask != 0)
        {
            clearTimeout(window.translationTask);
        }
        window.translationTask = setTimeout
        (
            () => translate(target),
            1000
        );
    }
);

function translate(target)
{
    target = target.trim();
    if (target != "")
    {
        console.log('Translated: ', target);
        fetch(`/Home/FetchTranslation?lang-from=en&lang-to=uk&original-text=${target}&action-button=fetch`)
            .then(r => r.json())
            .then(res => {
                console.log(res);
            });
    }
    window.translationTask = 0;
}