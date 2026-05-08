# Laboratório 03 — Caracterizando a Atividade de Code Review no GitHub
## Primeira Versão do Relatório — Hipóteses Iniciais

**Data de geração:** 08/05/2026 16:44
**Total de PRs coletados:** 10333
**PRs MERGED:** 7987
**PRs CLOSED:** 2346

---

## 1. Introdução

Este relatório apresenta a primeira versão da análise sobre a atividade de code review em repositórios populares do GitHub. O dataset foi construído a partir dos 200 repositórios mais populares (por estrelas) que possuíam pelo menos 100 PRs fechados (MERGED + CLOSED). Foram incluídos apenas PRs com pelo menos uma revisão e cujo tempo de análise foi superior a uma hora, excluindo assim revisões automáticas realizadas por bots ou ferramentas de CI/CD.

### Hipóteses Iniciais

Antes da análise estatística, elencamos nossas expectativas informais para cada questão de pesquisa:

**RQ 01 — Tamanho × Status do PR:**
Esperamos que PRs aceitos (MERGED) sejam, em geral, menores do que os rejeitados (CLOSED). PRs pequenos são mais fáceis de revisar, menos propensos a conflitos e tendem a ser aprovados com mais rapidez.

**RQ 02 — Tempo de Análise × Status do PR:**
Acreditamos que PRs MERGED tendam a ter um tempo de análise maior, pois passam por um processo de revisão mais cuidadoso. PRs CLOSED podem ser rejeitados rapidamente quando claramente inadequados, mas também podem arrastar-se por longos períodos sem consenso.

**RQ 03 — Descrição × Status do PR:**
Nossa hipótese é que PRs MERGED possuam descrições mais longas e detalhadas. Uma boa descrição facilita o entendimento do revisor e aumenta as chances de aprovação.

**RQ 04 — Interações × Status do PR:**
Esperamos que PRs MERGED tenham mais participantes e comentários, refletindo um processo colaborativo mais rico. PRs CLOSED podem ter poucas interações se forem rejeitados precocemente.

**RQ 05 — Tamanho × Número de Revisões:**
Acreditamos que PRs maiores (mais arquivos e linhas) exijam mais rodadas de revisão, pois apresentam mais pontos passíveis de feedback e correção.

**RQ 06 — Tempo de Análise × Número de Revisões:**
Esperamos correlação positiva: PRs com mais revisões tendem a demorar mais para serem finalizados, pois cada rodada envolve correção e reanálise.

**RQ 07 — Descrição × Número de Revisões:**
Hipótese: PRs com descrições mais longas podem precisar de menos revisões, pois o contexto claro reduz dúvidas. Alternativamente, descrições longas podem indicar maior complexidade e, portanto, mais revisões.

**RQ 08 — Interações × Número de Revisões:**
Esperamos forte correlação positiva entre o número de comentários/participantes e o número de revisões, pois PRs mais discutidos naturalmente passam por mais ciclos de revisão.

---

## 2. Metodologia

### 2.1 Criação do Dataset

Os dados foram coletados via API GraphQL do GitHub. Os critérios de seleção foram:

- **Repositórios:** 200 repositórios mais populares do GitHub (ordenados por estrelas).
- **Requisito mínimo:** pelo menos 100 PRs com status MERGED ou CLOSED.
- **Filtro de revisão:** apenas PRs com pelo menos uma revisão registrada.
- **Filtro de tempo:** apenas PRs cujo intervalo entre criação e fechamento/merge é superior a 1 hora (para excluir revisões automáticas de bots e ferramentas de CI/CD).

### 2.2 Métricas Coletadas

| Dimensão | Métrica | Campo no CSV |
|----------|---------|--------------|
| Tamanho | Número de arquivos alterados | `changed_files` |
| Tamanho | Total de linhas adicionadas | `additions` |
| Tamanho | Total de linhas removidas | `deletions` |
| Tempo de Análise | Horas entre criação e fechamento/merge | `analysis_time_hours` |
| Descrição | Número de caracteres do corpo do PR | `description_length` |
| Interações | Número de participantes | `participants_count` |
| Interações | Número de comentários (issue + review threads) | `comments_count` |
| Variável dependente A | Status final (MERGED / CLOSED) | `state` |
| Variável dependente B | Número de revisões | `reviews_count` |

### 2.3 Análise Estatística (planejada para Sprint 3)

As correlações entre métricas serão calculadas utilizando o **teste de correlação de Spearman**, por ser não-paramétrico e adequado para dados ordinais ou que não seguem distribuição normal — o que é esperado em métricas de repositórios de software (caudas longas, outliers). As comparações entre grupos MERGED e CLOSED utilizarão **valores medianos**, conforme indicado no enunciado.

---

## 3. Sumário do Dataset (Valores Medianos)

### 3.1 Visão Geral

| Métrica | Mediana Geral | Mediana MERGED | Mediana CLOSED |
|---------|:-------------:|:--------------:|:--------------:|
| Arquivos alterados | 2.00 | 2.00 | 1.00 |
| Linhas adicionadas | 20.00 | 20.00 | 21.00 |
| Linhas removidas | 4.00 | 5.00 | 2.00 |
| Tempo de análise (horas) | 16.28 | 13.04 | 38.93 |
| Comprimento da descrição (chars) | 837.00 | 830.00 | 862.00 |
| Participantes | 2.00 | 2.00 | 2.00 |
| Comentários | 2.00 | 2.00 | 3.00 |
| Revisões | 1.00 | 1.00 | 1.00 |

### 3.2 Distribuição por Status

- Total de PRs: **10333**
- MERGED: **7987** (77,3%)
- CLOSED: **2346** (22,7%)

---

## 4. Resultados Preliminares e Discussão das Hipóteses

Esta seção será expandida na Sprint 3, após a aplicação dos testes estatísticos e a criação das visualizações. Com base nos valores medianos apresentados na seção anterior, é possível realizar uma análise preliminar qualitativa:

### RQ 01 — Tamanho × Status

- Mediana (Número de arquivos alterados) MERGED: **2.00**
- Mediana (Número de arquivos alterados) CLOSED: **1.00**
- **Observação preliminar:** PRs CLOSED tendem a ser menores. Contradiz ou nuança a hipótese inicial.

### RQ 02 — Tempo de Análise × Status

- Mediana (Tempo de análise (horas)) MERGED: **13.04**
- Mediana (Tempo de análise (horas)) CLOSED: **38.93**
- **Observação preliminar:** PRs MERGED passam por revisão mais longa. Confirma a hipótese inicial.

### RQ 03 — Descrição × Status

- Mediana (Comprimento da descrição (chars)) MERGED: **830.00**
- Mediana (Comprimento da descrição (chars)) CLOSED: **862.00**
- **Observação preliminar:** PRs MERGED possuem descrições mais detalhadas. Confirma a hipótese inicial.

### RQ 04 — Interações × Status

- Mediana (Número de comentários) MERGED: **2.00**
- Mediana (Número de comentários) CLOSED: **3.00**
- **Observação preliminar:** PRs MERGED têm mais interações colaborativas. Confirma a hipótese inicial.


---

## 5. Próximos Passos (Sprint 3)

- Aplicar o teste de correlação de Spearman para todas as 8 RQs.
- Gerar visualizações (box plots, scatter plots) para cada par de variáveis.
- Confrontar os resultados com as hipóteses iniciais.
- Elaborar o relatório final completo.

---

*Relatório gerado automaticamente pela Sprint 2 do Laboratório 03.*
