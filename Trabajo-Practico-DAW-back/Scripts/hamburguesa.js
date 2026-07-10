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
  localStorage.clear();
  window.location.href = '../LogIn.html';
  });
});

function tienePrivilegios() {
  const privileges = JSON.parse(localStorage.getItem('privileges') || '[]');
  return privileges.includes('USERS_MANAGE');
}