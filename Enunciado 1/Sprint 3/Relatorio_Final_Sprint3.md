# Relatório Final — Laboratório 01: Características de Repositórios Populares

## 1. Introdução e hipóteses informais

Este relatório analisa os **1.000 repositórios com maior número de estrelas no GitHub** para responder às questões de pesquisa (RQs) do laboratório. As hipóteses informais são:

- **RQ 01 (idade):** Espera-se que repositórios populares sejam relativamente maduros/antigos (ex.: mediana de idade > 5 anos).
- **RQ 02 (contribuição externa):** Espera-se que recebam muitas contribuições via pull requests (mediana de PRs aceitas elevada).
- **RQ 03 (releases):** Espera-se que lancem releases com certa frequência (mediana de releases > 0 ou relativamente alta).
- **RQ 04 (atualização):** Espera-se que sejam atualizados com frequência (mediana de dias desde a última atualização baixa).
- **RQ 05 (linguagem):** Espera-se que predominem linguagens muito usadas (JavaScript/TypeScript, Python, etc.).
- **RQ 06 (issues fechadas):** Espera-se um percentual alto de issues fechadas em relação ao total (ex.: mediana da razão > 70%).
- **RQ 07 (bônus):** Sistemas em linguagens mais populares podem receber mais PRs, mais releases e serem atualizados com mais frequência.

## 2. Metodologia

Os dados foram obtidos via **API GraphQL do GitHub** (Sprints 1 e 2), com paginação para 1.000 repositórios ordenados por número de estrelas. Os dados foram exportados em CSV (separador `;`). Para cada RQ numérica, foi calculada a **mediana** dos valores; para a RQ 05 (linguagem), foi feita **contagem por categoria**. A análise e visualização foram realizadas no Sprint 3 com um script em C# (.NET).

## 3. Resultados

Total de repositórios analisados: **1000**.

### RQ 01 — Sistemas populares são maduros/antigos?
- **Mediana da idade do repositório:** 8.45 anos (n = 1000).

### RQ 02 — Sistemas populares recebem muita contribuição externa?
- **Mediana de pull requests aceitas:** 737 (n = 1000).

### RQ 03 — Sistemas populares lançam releases com frequência?
- **Mediana de total de releases:** 40 (n = 1000).

### RQ 04 — Sistemas populares são atualizados com frequência?
- **Mediana de dias desde a última atualização:** 0 dias (n = 1000). Quanto menor, mais recente a atualização.

### RQ 05 — Sistemas populares são escritos nas linguagens mais populares?
Contagem por linguagem (primeiras 20):

| Linguagem | Quantidade de repositórios |
|-----------|---------------------------|
| Python | 200 |
| TypeScript | 158 |
| JavaScript | 117 |
| (não detectada) | 95 |
| Go | 77 |
| Rust | 53 |
| Java | 47 |
| C++ | 46 |
| C | 25 |
| Jupyter Notebook | 23 |
| Shell | 21 |
| HTML | 18 |
| Ruby | 12 |
| C# | 11 |
| Kotlin | 10 |
| CSS | 8 |
| Vue | 7 |
| PHP | 7 |
| Swift | 7 |
| Dart | 6 |

### RQ 06 — Sistemas populares possuem alto percentual de issues fechadas?
- **Mediana da razão (issues fechadas / total de issues):** 87.8% (n = 958).

### RQ 07 (bônus) — Por linguagem: mais contribuição, releases e atualizações?
Mediana de PRs aceitas, total de releases e dias desde última atualização por linguagem (top 15):

| Linguagem | N | Med. PRs | Med. Releases | Med. Dias |
|-----------|---|----------|---------------|-----------|
| Python | 200 | 629 | 24 | 0 |
| TypeScript | 158 | 2579 | 160 | 0 |
| JavaScript | 117 | 575 | 36 | 0 |
| (não detectada) | 95 | 129 | 0 | 0 |
| Go | 77 | 1690 | 133 | 0 |
| Rust | 53 | 2393 | 78 | 0 |
| Java | 47 | 605 | 42 | 0 |
| C++ | 46 | 974 | 64 | 0 |
| C | 25 | 145 | 39 | 0 |
| Jupyter Notebook | 23 | 88 | 0 | 0 |
| Shell | 21 | 494 | 26 | 0 |
| HTML | 18 | 310 | 0 | 0 |
| Ruby | 12 | 4768 | 14 | 0 |
| C# | 11 | 5168 | 119 | 0 |
| Kotlin | 10 | 400 | 54 | 0 |

## 4. Discussão

Compare as **hipóteses** da introdução com os **valores obtidos**:
- **RQ 01:** A mediana de idade indica se os repositórios populares tendem a ser maduros; valores em torno de 8–10 anos reforçam que popularidade muitas vezes acompanha maturidade.
- **RQ 02 e 03:** Medianas de PRs e releases variam muito; repositórios de documentação/lista (awesome, listas) podem ter poucas releases mas muitas estrelas.
- **RQ 04:** Mediana de dias baixa indica que a maioria dos repositórios populares é atualizada com frequência.
- **RQ 05:** A tabela por linguagem mostra quais linguagens predominam entre os 1.000 mais estrelados.
- **RQ 06:** Uma mediana alta da razão de issues fechadas sugere que projetos populares tendem a manter as issues em dia.
- **RQ 07:** Comparando as medianas por linguagem, é possível ver se linguagens mais representadas (ex.: JavaScript, Python) têm medianas de PRs, releases e dias até atualização diferentes das demais.
