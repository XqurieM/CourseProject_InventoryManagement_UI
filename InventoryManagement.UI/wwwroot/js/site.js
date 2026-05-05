document.addEventListener('DOMContentLoaded', function () {
  const toggle = document.getElementById('themeToggle');
  if (toggle) {
    toggle.addEventListener('click', function (e) {
      e.preventDefault();
      const html = document.documentElement;
      const dark = html.getAttribute('data-bs-theme') === 'dark';
      html.setAttribute('data-bs-theme', dark ? 'light' : 'dark');
      toggle.innerHTML = dark ? '<i class="fa-solid fa-moon"></i>' : '<i class="fa-solid fa-sun"></i>';
    });
  }
  document.querySelectorAll('[data-sortable="true"]').forEach(function (el) {
    if (window.Sortable) Sortable.create(el, { handle: '.rule-handle, .field-handle', animation: 150 });
  });
});
