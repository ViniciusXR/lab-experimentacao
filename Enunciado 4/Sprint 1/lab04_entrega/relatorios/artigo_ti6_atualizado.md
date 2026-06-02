# Artigo TI6 — Atualização Lab04 (Seções 3 e 4)

*Inteligência de dados aplicada à segurança: priorização estratégica de CWEs*

---

## 3. Metodologia — Caracterização do Dataset

A base compreende **253.802 CVEs distintos** e **278.912 relações CVE↔CWE** extraídas da NIST NVD (1999–2025), abrangendo **749 CWEs** distintas com cobertura CVSS de **99,99%**.

**Figura 1** apresenta o volume anual de CVEs distintos, evidenciando crescimento acelerado a partir de 2017 e pico em 2025 (~45 mil CVEs), o que sustenta a relevância do problema de fadiga de alertas.

**Figura 2** mostra a distribuição por classe de severidade CVSS no período completo: MEDIUM concentra 45,5% das CVEs, HIGH 39,0% e CRITICAL 11,5%.

**Figura 3** detalha a composição percentual de severidade por ano, revelando aumento da fatia HIGH/CRITICAL após 2016 (entrada do CVSS v3).

**Figura 4** exibe a distribuição do score CVSS (multimodal, com picos em ~5,5; ~7,5; ~9,8). A mediana global (7,00) é preferível à média (6,88) por ser robusta a outliers.

---

## 4. Resultados

### 4.1 RQ1 — Frequência

**Figura 5** (Pareto) confirma concentração extrema: **38 CWEs** respondem por **80%** das relações. CWE-79 (XSS) lidera isoladamente com 41.171 ocorrências.

**Figura 6** mostra a evolução temporal das 10 CWEs mais frequentes; XSS dispara após 2017.

**Figura 7** (Curva de Lorenz) quantifica a desigualdade (Gini = 0,92).

### 4.2 RQ2 — Severidade

**Figura 8** apresenta CWEs com maior CVSS médio (n ≥ 30); cluster em ~9,8.
**Figura 9** complementa com % HIGH/CRITICAL por CWE. Kruskal-Wallis: p < 0,001.

### 4.3 RQ3 — Interação Frequência × Severidade

**Figura 10** (dispersão) mostra independência global (Spearman ρ ≈ −0,02, p = 0,62), mas **53 CWEs** no quadrante Dupla Ameaça combinam alta frequência e alta severidade.

**Figura 11** ranqueia CWEs pelo score de risco composto; **Figura 12** resume os quadrantes.

**Tabela 1** lista a fila de priorização estratégica (53 CWEs Dupla Ameaça), liderada por SQL Injection (CWE-89), Out-of-bounds Write (CWE-787) e Buffer Overflow (CWE-119).

---

## Síntese GQM

| Pergunta | Veredito | Evidência |
|----------|----------|-----------|
| Frequência: quais CWEs mais se repetem? | CONFIRMADA | 38 CWEs cobrem 80% das CVEs; Gini = 0,92 |
| Severidade: quais CWEs têm maior potencial de dano? | CONFIRMADA | Cluster de ~15 CWEs com CVSS médio ≈ 9,8 (100% CRITICAL); Kruskal-Wallis p < 0,001 |
| Interação: frequência e severidade andam juntas? | CONFIRMADA (com nuance) | Spearman ρ ≈ -0,02 (independentes), mas 53 CWEs em Dupla Ameaça |

## Testes estatísticos

| Teste | p-valor | Resultado |
|-------|---------|-----------|
| Pareto (M2) | — | CONFIRMA concentração (Pareto) |
| Kruskal-Wallis | < 0,001 | CONFIRMA variação de severidade |
| Spearman (M6) | 0,621 | CONFIRMA independência (risco bimodal) |
| Mann-Whitney U | < 0,001 | CONFIRMA distinção entre grupos |
