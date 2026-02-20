/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    "./Components/**/*.razor",
    "./**/*.razor"
  ],
  theme: {
    extend: {
      colors: {
        primary: '#195de6',
        'background-light': '#f6f6f8',
        'background-dark': '#111621',
        steel: '#243047',
        'border-gray': '#344465'
      },
      fontFamily: {
        display: ['Public Sans', 'sans-serif']
      }
    }
  },
  plugins: []
}
