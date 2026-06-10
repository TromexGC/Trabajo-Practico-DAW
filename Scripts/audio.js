document.addEventListener('DOMContentLoaded', () => {
    // 1. Seleccionamos la etiqueta de audio
    const miAudio = document.querySelector('audio');

    // 2. Creamos una función para reproducirlo
    function reproducirAudio() {
        // .play() devuelve una "Promesa" en JS, por eso se maneja de esta forma
        miAudio.play()
            .then(() => {
                // Si se pudo reproducir, removemos el evento para que no intente darle play en cada clic posterior
                document.removeEventListener('click', reproducirAudio);
            })
            .catch(error => {
                console.log("El navegador bloqueó el autoplay todavía:", error);
            });
    }

    // 3. Escuchamos el primer clic del usuario en cualquier parte de la página
    document.addEventListener('click', reproducirAudio);
});