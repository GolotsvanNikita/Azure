window.translationTask = 0;
let checkbox = document.getElementById('switchSelection');
let isChecked = false;
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

checkbox.addEventListener('change', () =>
{
    if (checkbox.checked)
    {
        isChecked = true;
    }
    else
    {
        isChecked = false;
    }
});

function translate(target) {
    target = target.trim();
    if (target != "")
    {
        if (isChecked != false)
        {
            const langFrom = document.querySelector('select[name="lang-from"]').value;
            const langTo = document.querySelector('select[name="lang-to"]').value;

            console.log('Translated: ', target);
            fetch(`/Home/FetchTranslation?lang-from=${langFrom}&lang-to=${langTo}&original-text=${target}&action-button=fetch`)
                .then(r => r.json())
                .then(res => {
                    console.log(res);
                });
        }
    }
    window.translationTask = 0;
}