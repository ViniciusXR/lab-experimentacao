# Lab05 - Entrega

Esta pasta consolida a atividade do **Laboratório 05 - GraphQL vs REST**.

## Como reproduzir

Execute, a partir da raiz do repositório:

```powershell
dotnet run --project "Enunciado 5\Sprint 1\Sprint1.csproj"
dotnet run --project "Enunciado 5\Sprint 2\Sprint2.csproj"
dotnet run --project "Enunciado 5\Sprint 3\Sprint3.csproj"
```

## Entregáveis principais

- `relatorios/sprint1_plano_experimento.md`: desenho do experimento.
- `relatorios/protocolo_replicacao.md`: protocolo de reprodução.
- `dados/medicoes_lab05.csv`: 1.200 medições coletadas.
- `dados/resumo_estatistico.csv`: estatísticas descritivas por cenário e tratamento.
- `dados/testes_estatisticos.csv`: Mann-Whitney U, Cliff's delta e vereditos.
- `relatorios/relatorio_final_lab05.md`: relatório final em Markdown.
- `relatorios/relatorio_final_lab05.pdf`: relatório final em PDF.
- `dashboards/dashboard_lab05.html`: dashboard visual da Sprint 3.

## Resultado sintetizado

No experimento controlado, GraphQL apresentou menor mediana agregada de tempo e menor tamanho de resposta que REST. Os resultados apoiam as hipóteses alternativas H1-1 e H1-2 no cenário experimental definido.
