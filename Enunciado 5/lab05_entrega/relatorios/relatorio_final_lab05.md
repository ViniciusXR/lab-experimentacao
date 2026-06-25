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

- .NET: 10.0.9
- Sistema operacional: Microsoft Windows 10.0.26200
- Arquitetura: X64
- Processadores logicos: 16
- Semente do dataset: 20260624
- Semente dos trials: 5052026

## 3. Resultados descritivos

| Cenario | Tratamento | n | Mediana tempo (ms) | P95 tempo (ms) | Mediana tamanho (bytes) |
|---|---:|---:|---:|---:|---:|
| Q1 - Lista de repositorios | GraphQL | 120 | 0,1327 | 0,2166 | 2.552 |
| Q1 - Lista de repositorios | REST | 120 | 0,5823 | 0,8900 | 21.239 |
| Q2 - Detalhe do repositorio | GraphQL | 120 | 0,0186 | 0,0394 | 185 |
| Q2 - Detalhe do repositorio | REST | 120 | 0,2556 | 0,3830 | 12.440 |
| Q3 - Issues recentes | GraphQL | 120 | 0,3206 | 0,4799 | 7.633 |
| Q3 - Issues recentes | REST | 120 | 0,9658 | 1,3784 | 54.394 |
| Q4 - Contribuidores | GraphQL | 120 | 0,0941 | 0,1532 | 2.053 |
| Q4 - Contribuidores | REST | 120 | 0,4167 | 0,5952 | 21.183 |
| Q5 - Visao aninhada | GraphQL | 120 | 0,0860 | 0,1378 | 1.629 |
| Q5 - Visao aninhada | REST | 120 | 0,7190 | 0,9822 | 34.223 |

## 4. Testes e respostas as RQs

| Cenario | Reducao tempo GraphQL vs REST | p tempo | Cliff tempo | Reducao tamanho GraphQL vs REST | p tamanho | Cliff tamanho |
|---|---:|---:|---:|---:|---:|---:|
| Q1 - Lista de repositorios | 77,21% | < 0,0001 | -1,000 | 87,98% | < 0,0001 | -1,000 |
| Q2 - Detalhe do repositorio | 92,70% | < 0,0001 | -1,000 | 98,51% | < 0,0001 | -1,000 |
| Q3 - Issues recentes | 66,80% | < 0,0001 | -1,000 | 85,97% | < 0,0001 | -1,000 |
| Q4 - Contribuidores | 77,42% | < 0,0001 | -1,000 | 90,31% | < 0,0001 | -1,000 |
| Q5 - Visao aninhada | 88,04% | < 0,0001 | -1,000 | 95,24% | < 0,0001 | -1,000 |
| **Todos** | **81,54%** | **< 0,0001** | **-0,919** | **90,33%** | **< 0,0001** | **-1,000** |

### RQ1 - Respostas GraphQL sao mais rapidas?

GraphQL apresentou menor mediana de tempo em 5 de 5 cenarios com p < 0,05. No agregado, a reducao foi de 81,54% (p < 0,0001). Portanto, a hipotese alternativa H1-1 e apoiada para este experimento.

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

No experimento controlado, GraphQL reduziu a mediana agregada de tempo em 81,54% e a mediana agregada de tamanho em 90,33%. Assim, as evidencias apoiam H1-1 e H1-2 neste desenho experimental.
