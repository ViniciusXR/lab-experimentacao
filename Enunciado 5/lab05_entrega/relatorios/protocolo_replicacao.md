# Protocolo de Replicacao - Lab05

## Ambiente

- SDK: .NET 10.
- Sistema operacional: registrado automaticamente na Sprint 2.
- Execucao sem chamadas de rede e sem dependencias externas.
- Semente do dataset: 20260624.
- Semente de randomizacao dos trials: 5052026.

## Passos

1. Executar `dotnet run --project "Enunciado 5/Sprint 1/Sprint1.csproj"` para gerar o plano e os arquivos de preparacao.
2. Executar `dotnet run --project "Enunciado 5/Sprint 2/Sprint2.csproj"` para coletar medicoes, calcular estatisticas e produzir o relatorio final.
3. Executar `dotnet run --project "Enunciado 5/Sprint 3/Sprint3.csproj"` para gerar o dashboard HTML.

## Saidas esperadas

- `lab05_entrega/dados/medicoes_lab05.csv`
- `lab05_entrega/dados/resumo_estatistico.csv`
- `lab05_entrega/dados/testes_estatisticos.csv`
- `lab05_entrega/relatorios/relatorio_final_lab05.md`
- `lab05_entrega/dashboards/dashboard_lab05.html`

## Criterios de reproducibilidade

A execucao deve produzir a mesma quantidade de trials e a mesma estrutura de arquivos. Pequenas diferencas nos tempos sao esperadas porque dependem da maquina, carga do sistema e runtime.