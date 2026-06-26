# Protocolo de Replicacao - Lab05

## Ambiente

- SDK: .NET 10.
- Python: 3.10 ou superior, com `pandas`, `matplotlib` e `seaborn` (ver `Enunciado 5/dashboard/requirements.txt`).
- Sistema operacional: registrado automaticamente na Sprint 2.
- Execucao sem chamadas de rede e sem dependencias externas.
- Semente do dataset: 20260624.
- Semente de randomizacao dos trials: 5052026.

## Passos

1. Executar `dotnet run --project "Enunciado 5/Sprint 1/Sprint1.csproj"` para gerar o plano e os arquivos de preparacao.
2. Executar `dotnet run --project "Enunciado 5/Sprint 2/Sprint2.csproj"` para coletar medicoes, calcular estatisticas e produzir o relatorio final.
3. Executar `dotnet run --project "Enunciado 5/Sprint 3/Sprint3.csproj"` para gerar o dashboard HTML (versao C#/SVG).
4. Instalar dependencias Python com `pip install -r "Enunciado 5/dashboard/requirements.txt"`.
5. Executar `python "Enunciado 5/dashboard/gerar_dashboard.py"` para processar os dados com pandas e gerar os graficos (matplotlib/seaborn), as tabelas e o dashboard do Passo 6.

## Saidas esperadas

- `lab05_entrega/dados/medicoes_lab05.csv`
- `lab05_entrega/dados/resumo_estatistico.csv`
- `lab05_entrega/dados/testes_estatisticos.csv`
- `lab05_entrega/relatorios/relatorio_final_lab05.md`
- `lab05_entrega/dashboards/dashboard_lab05.html`
- `lab05_entrega/dashboards/dashboard_lab05_python.html`
- `lab05_entrega/dashboards/graficos/*.png`
- `lab05_entrega/dashboards/tabelas/*.csv`

## Criterios de reproducibilidade

A execucao deve produzir a mesma quantidade de trials e a mesma estrutura de arquivos. Pequenas diferencas nos tempos sao esperadas porque dependem da maquina, carga do sistema e runtime.