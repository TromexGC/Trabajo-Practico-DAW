document.addEventListener('DOMContentLoaded', () => {
    // 1. Seleccionamos todos los formularios y todos los enlaces de alternancia
    const mainContainer = document.querySelector('main');
    const allForms = mainContainer.querySelectorAll('form');
    const toggles = document.querySelectorAll('.form-toggle');

    // 2. Recorremos cada uno de los enlaces que tengan la clase .form-toggle
    toggles.forEach(toggle => {
        toggle.addEventListener('click', (e) => {
            e.preventDefault(); // Evitamos que recargue la página

            // Capturamos el ID del formulario destino desde el atributo data-target
            const targetFormId = toggle.getAttribute('data-target');
            const targetForm = document.getElementById(targetFormId);

            if (targetForm) {
                // Ocultamos absolutamente todos los formularios del contenedor
                allForms.forEach(form => {
                    form.style.display = 'none';
                });

                // Mostramos únicamente el formulario solicitado
                // Usamos 'flex' para mantener tu estructura de Flexbox
                targetForm.style.display = 'flex'; 
            }
        });
    });
});

