# Lab04 — Entrega Completa (Sprints 1, 2 e 3)

Gerado por `dotnet run` na pasta Sprint 1.

## Estrutura

```
lab04_entrega/
├── dashboards/
│   ├── sprint1.html          ← apresentação Sprint 1
│   ├── sprint2.html          ← apresentação Sprint 2
│   └── dashboard_final.html  ← ENTREGA FINAL (abrir no navegador)
├── graficos/
│   ├── p1_caracterizacao/    ← 4 gráficos
│   ├── p2_rq1/               ← 4 gráficos
│   ├── p3_rq2/               ← 2 gráficos
│   └── p4_rq3/               ← 3 gráficos
└── relatorios/
    ├── sprint1_caracterizacao.md
    ├── sprint2_rq1_rq2.md
    ├── sprint3_relatorio_final.md
    ├── artigo_ti6_atualizado.md   ← copiar para o artigo TI6
    └── checklist_entrega.md
```

## Como usar

1. **Apresentar em aula:** abra `dashboards/sprint1.html` ou `sprint2.html` conforme a sprint.
2. **Entrega final:** abra `dashboards/dashboard_final.html` → **Ctrl+P** → Salvar como PDF.
3. **Artigo TI6:** use o texto de `relatorios/artigo_ti6_atualizado.md` nas Seções 3 e 4.
4. **Power BI (se o professor exigir):** importe `dashboard_powerbi.xlsx` e siga `GUIA_DASHBOARD_POWERBI.md`.

## Regenerar tudo

```powershell
cd "C:\Users\estagio.analise\trabalho lab teste\Enunciado 4\Sprint 1"
dotnet run
```
