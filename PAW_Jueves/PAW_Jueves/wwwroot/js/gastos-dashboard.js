/*
  Gráficos del dashboard de gastos (ApexCharts, ya cargado en _Layout).
  Los datos vienen del servidor en <script type="application/json" id="datos-dashboard">,
  así no se mezcla C# con JavaScript y no hace falta otra llamada a la API.
*/
(function () {
  'use strict';

  const fuente = document.getElementById('datos-dashboard');
  if (!fuente || typeof ApexCharts === 'undefined') return;

  const datos = JSON.parse(fuente.textContent);
  const colorTexto = getComputedStyle(document.body).color;
  const formato = new Intl.NumberFormat('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const dinero = function (valor) { return datos.moneda + formato.format(valor); };

  const comunes = {
    chart: { fontFamily: 'inherit', foreColor: colorTexto, toolbar: { show: false } },
    tooltip: { y: { formatter: dinero } },
    legend: { labels: { colors: colorTexto } }
  };

  // Dona: gasto por categoría con el color de cada categoría
  const elCategorias = document.getElementById('grafico-categorias');
  if (elCategorias && datos.categorias.length) {
    new ApexCharts(elCategorias, Object.assign({}, comunes, {
      chart: Object.assign({}, comunes.chart, { type: 'donut', height: 300 }),
      series: datos.categorias.map(function (c) { return c.total; }),
      labels: datos.categorias.map(function (c) { return c.nombre; }),
      colors: datos.categorias.map(function (c) { return c.color; }),
      legend: Object.assign({}, comunes.legend, { position: 'bottom' }),
      dataLabels: { enabled: false },
      stroke: { width: 0 }
    })).render();
  }

  // Barras: ingresos vs. gastos de los últimos 6 meses
  const elTendencia = document.getElementById('grafico-tendencia');
  if (elTendencia) {
    new ApexCharts(elTendencia, Object.assign({}, comunes, {
      chart: Object.assign({}, comunes.chart, { type: 'bar', height: 300 }),
      series: [
        { name: 'Ingresos', data: datos.tendencia.map(function (t) { return t.ingresos; }) },
        { name: 'Gastos', data: datos.tendencia.map(function (t) { return t.gastos; }) }
      ],
      xaxis: { categories: datos.tendencia.map(function (t) { return t.mes; }) },
      yaxis: { labels: { formatter: function (v) { return datos.moneda + Math.round(v).toLocaleString('es-CR'); } } },
      colors: ['#40c057', '#fa5252'],
      plotOptions: { bar: { borderRadius: 4, columnWidth: '55%' } },
      dataLabels: { enabled: false },
      grid: { borderColor: 'rgba(128,128,128,0.2)' }
    })).render();
  }
})();
