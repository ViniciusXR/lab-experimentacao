# Relatorio Final - Lab05: GraphQL vs REST

## 1. Introducao

Este relatorio apresenta um experimento controlado para comparar REST e GraphQL em dois criterios quantitativos: tempo de resposta e tamanho do payload. O objetivo e responder as perguntas RQ1 e RQ2 propostas no enunciado do Laboratorio 05.

As hipoteses alternativas adotadas foram: GraphQL possui menor tempo de resposta que REST (H1-1) e GraphQL possui menor tamanho de resposta que REST (H1-2).

## 2. Metodologia

- **Tipo de projeto:** intra-sujeitos; cada cenario foi executado com os dois tratamentos.
- **Tratamentos:** REST com recursos completos e GraphQL com projecao explicita de campos.
- **Objetos experimentais:** catalogo local deterministico com repositorios, issues, releases e contribuidores.
- **Medicoes:** 1.200 trials coletados, com 120 repeticoes por tratamento em cada cenario.
- **Aquecimento:** 20 execucoes por tratamento/cenario antes da coleta.
- **Metrica RQ1:** tempo de resposta em milissegundos, incluindo montagem, serializacao UTF-8 e consumo JSON.
- **Metrica RQ2:** tamanho da resposta em bytes UTF-8.
- **Teste estatistico:** Mann-Whitney U bilateral, alfa = 0,05, com Cliff's delta como tamanho de efeito.

### Ambiente de execucao

- .NET: 10.0.3
- Sistema operacional: macOS 26.5.1
- Arquitetura: Arm64
- Processadores logicos: 10
- Semente do dataset: 20260624
- Semente dos trials: 5052026

## 3. Resultados descritivos

| Cenario | Tratamento | n | Mediana tempo (ms) | P95 tempo (ms) | Mediana tamanho (bytes) |
|---|---:|---:|---:|---:|---:|
| Q1 - Lista de repositorios | GraphQL | 120 | 0,0307 | 0,0344 | 2.552 |
| Q1 - Lista de repositorios | REST | 120 | 0,1383 | 0,1459 | 21.239 |
| Q2 - Detalhe do repositorio | GraphQL | 120 | 0,0033 | 0,0050 | 185 |
| Q2 - Detalhe do repositorio | REST | 120 | 0,0486 | 0,0552 | 12.440 |
| Q3 - Issues recentes | GraphQL | 120 | 0,0775 | 0,0833 | 7.633 |
| Q3 - Issues recentes | REST | 120 | 0,1993 | 0,2131 | 54.394 |
| Q4 - Contribuidores | GraphQL | 120 | 0,0212 | 0,0231 | 2.053 |
| Q4 - Contribuidores | REST | 120 | 0,0873 | 0,0911 | 21.183 |
| Q5 - Visao aninhada | GraphQL | 120 | 0,0188 | 0,0217 | 1.629 |
| Q5 - Visao aninhada | REST | 120 | 0,1425 | 0,1537 | 34.223 |

## 4. Testes e respostas as RQs

| Cenario | Reducao tempo GraphQL vs REST | p tempo | Cliff tempo | Reducao tamanho GraphQL vs REST | p tamanho | Cliff tamanho |
|---|---:|---:|---:|---:|---:|---:|
| Q1 - Lista de repositorios | 77,79% | < 0,0001 | -1,000 | 87,98% | < 0,0001 | -1,000 |
| Q2 - Detalhe do repositorio | 93,15% | < 0,0001 | -1,000 | 98,51% | < 0,0001 | -1,000 |
| Q3 - Issues recentes | 61,10% | < 0,0001 | -1,000 | 85,97% | < 0,0001 | -1,000 |
| Q4 - Contribuidores | 75,71% | < 0,0001 | -1,000 | 90,31% | < 0,0001 | -1,000 |
| Q5 - Visao aninhada | 86,83% | < 0,0001 | -1,000 | 95,24% | < 0,0001 | -1,000 |
| **Todos** | **84,61%** | **< 0,0001** | **-0,918** | **90,33%** | **< 0,0001** | **-1,000** |

### RQ1 - Respostas GraphQL sao mais rapidas?

GraphQL apresentou menor mediana de tempo em 5 de 5 cenarios com p < 0,05. No agregado, a reducao foi de 84,61% (p < 0,0001). Portanto, a hipotese alternativa H1-1 e apoiada para este experimento.

### RQ2 - Respostas GraphQL tem tamanho menor?

GraphQL apresentou menor tamanho de resposta em 5 de 5 cenarios com p < 0,05. No agregado, a reducao foi de 90,33% (p < 0,0001). Portanto, a hipotese alternativa H1-2 e apoiada para este experimento.

## 5. Discussao

O experimento favorece GraphQL principalmente por reduzir over-fetching: os payloads GraphQL incluem apenas os campos declarados nas consultas, enquanto os endpoints REST retornam recursos completos. Essa diferenca afeta diretamente o tamanho das respostas e, indiretamente, o tempo gasto em serializacao e parsing JSON.

A diferenca de tempo deve ser interpretada com cuidado. Como o experimento foi local, ele isola custo de payload e processamento, mas nao captura latencia de rede, cache HTTP, infraestrutura de servidores reais ou custo de validacao de um servidor GraphQL completo. Ainda assim, o controle local permite observar o efeito direto do volume de dados retornado.

## 6. Ameacas a validade

- **Interna:** pequenas variacoes do sistema operacional podem afetar tempos submilissegundos; a mediana e o alto numero de repeticoes reduzem esse risco.
- **Externa:** APIs reais podem usar cache, compressao, paginacao e resolvedores complexos. Portanto, os resultados representam o cenario controlado, nao uma conclusao universal.
- **Construto:** GraphQL foi modelado pelo beneficio de projecao de campos; uma implementacao com parser, validacao e resolvedores reais pode adicionar overhead.
- **Conclusao:** o teste de Mann-Whitney e apropriado para dados nao normais, mas tamanho de efeito e relevancia pratica devem ser considerados junto ao p-valor.

## 7. Conclusao

No experimento controlado, GraphQL reduziu a mediana agregada de tempo em 84,61% e a mediana agregada de tamanho em 90,33%. Assim, as evidencias apoiam H1-1 e H1-2 neste desenho experimental.
