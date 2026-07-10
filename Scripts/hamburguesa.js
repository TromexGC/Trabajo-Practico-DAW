document.addEventListener('DOMContentLoaded', () => {
  const btnHamburguesa = document.getElementById('btn-hamburguesa');
  const menuLateral = document.getElementById('menu-lateral');
  const overlay = document.getElementById('overlay-menu');
  const btnLogout = document.getElementById('btn-logout');

  // Mostrar u ocultar opciones de admin según privilegios
  if (tienePrivilegios()) {
    document.querySelectorAll('.solo-admin').forEach(item => {
      item.hidden = false;
    });
  }

  function abrirMenu() {
    menuLateral.classList.add('abierto');
    btnHamburguesa.classList.add('activo');
    btnHamburguesa.setAttribute('aria-expanded', 'true');
    overlay.hidden = false;
  }

  function cerrarMenu() {
    menuLateral.classList.remove('abierto');
    btnHamburguesa.classList.remove('activo');
    btnHamburguesa.setAttribute('aria-expanded', 'false');
    overlay.hidden = true;
  }

  btnHamburguesa.addEventListener('click', () => {
    const estaAbierto = menuLateral.classList.contains('abierto');
    estaAbierto ? cerrarMenu() : abrirMenu();
  });

  overlay.addEventListener('click', cerrarMenu);

  btnLogout.addEventListener('click', (e) => {
    e.preventDefault();
    localStorage.removeItem('token'); // o el nombre que uses para guardar el token
    window.location.href = 'login.html';
  });
});

function tienePrivilegios() {
  // Placeholder: acá vas a decodificar el JWT o leer el dato guardado
  // en localStorage/sessionStorage cuando confirmes con tu compañero
  // el formato exacto del token.
  const token = localStorage.getItem('token');
  if (!token) return false;

  // Ejemplo de cómo se vería decodificando un JWT (sin librerías):
  // const payload = JSON.parse(atob(token.split('.')[1]));
  // return payload.privileges && payload.privileges.length > 0;

  return false; // por ahora, hardcodeado
}