# Guia do Dashboard no Power BI — Priorização Estratégica de CWEs (Lab04 / TI6)

Este guia leva você do zero ao **dashboard final salvo em PDF**, atendendo exatamente o que o
*Laboratório 04 — Visualização de dados utilizando uma ferramenta de BI* pede:

1. **Caracterização do dataset** (incluindo os **subgrupos** usados nas análises comparativas);
2. **Uma ou mais visualizações por questão de pesquisa (RQ)** do GQM, de forma **auto-explicativa**
   (a pergunta aparece e, logo abaixo, os gráficos que a respondem);
3. **Exportação do dashboard em PDF** + indicação de onde inserir os gráficos no **artigo de TI6**.

> Os dados já foram preparados para você. Não é preciso mexer em Python: basta importar os
> arquivos da pasta `dados/` (ou o `dashboard_powerbi.xlsx`) no Power BI e montar os visuais
> seguindo o passo a passo abaixo.

---

## 0. O que já está pronto nesta pasta

```
PowerBI/
├── dashboard_powerbi.xlsx        ← TODAS as tabelas em 1 arquivo (1 aba por tabela) — recomendado
├── dados/                        ← as mesmas tabelas em CSV (UTF-8, separador vírgula, decimal ponto)
│   ├── dim_cwe.csv
│   ├── kpis_gerais.csv
│   ├── caracterizacao_por_ano.csv
│   ├── caracterizacao_por_severidade.csv
│   ├── caracterizacao_severidade_por_ano.csv
│   ├── distribuicao_cvss.csv
│   ├── rq1_frequencia_cwe.csv
│   ├── rq1_tendencia_temporal.csv
│   ├── rq1_curva_lorenz.csv
│   ├── rq1_persistencia_ranking.csv
│   ├── rq2_severidade_cwe.csv
│   ├── rq3_dispersao_quadrante.csv
│   ├── rq3_score_risco.csv
│   ├── rq3_quadrantes_resumo.csv
│   ├── rq3_overlap_top10.csv
│   ├── lista_priorizacao.csv
│   ├── gqm_perguntas_metricas.csv
│   └── estatisticas_testes.csv
├── gerar_dados_powerbi.py        ← script que recriou as tabelas a partir de codigo/data (reprodutibilidade)
├── GUIA_DASHBOARD_POWERBI.md     ← este guia (Power BI, inclui publicação online — Seção 11)
└── GUIA_DASHBOARD_LOOKER_STUDIO.md ← guia equivalente para o Google Looker Studio (100% web, grátis)
```

### Mapa das entregas por Sprint

| Sprint | Entrega | Páginas deste guia |
|---|---|---|
| **Lab04S01** | Caracterização do dataset + apresentação | Página 1 (Seção 5) |
| **Lab04S02** | Visualizações das RQ1 e RQ2 + apresentação | Páginas 2 e 3 (Seções 6 e 7) |
| **Lab04S03** | Dashboard final (tudo) + artigo TI6 atualizado | Tudo + Seções 10 e 11 |

---

## 1. O dataset e as perguntas (contexto que o dashboard precisa contar)

**Objeto de estudo:** todas as relações CVE↔CWE da base **NIST NVD**, período **1999–2025**.
Cada CVE (vulnerabilidade) é classificada por uma ou mais **CWE** (categoria de fraqueza) e
possui um score de severidade **CVSS** (0–10).

- **253.802 CVEs distintos** · **278.912 relações CVE↔CWE** · **749 CWEs** · cobertura CVSS **99,99%**.
- **CVSS:** faixas LOW (0,1–3,9), MEDIUM (4,0–6,9), HIGH (7,0–8,9), CRITICAL (9,0–10,0).

**Perguntas de pesquisa (GQM):**

| RQ | Pergunta | Métricas | Veredito |
|---|---|---|---|
| **RQ1 — Frequência** | Quais CWEs mais se repetem na base? | M1 (frequência), M2 (Pareto), M7 (tendência), M9 (Gini), M10 (persistência) | **CONFIRMA**: 38 CWEs cobrem 80% das CVEs |
| **RQ2 — Severidade** | Quais CWEs têm maior potencial de dano? | M3 (CVSS médio), M4 (% HIGH/CRITICAL), Kruskal-Wallis | **CONFIRMA**: cluster de ~15 CWEs com CVSS ≈ 9,8 |
| **RQ3 — Interação** | Frequente é o mesmo que perigoso? | M5 (score de risco), M6 (Spearman), M8 (quadrante), M11 (overlap) | **CONFIRMA com nuance**: ρ ≈ 0 (independentes), mas **53 CWEs em "Dupla Ameaça"** |

> **Subgrupos usados nas comparações** (o lab exige caracterizar o todo **e** os subgrupos):
> (a) **classe de severidade** (LOW/MEDIUM/HIGH/CRITICAL); (b) **quadrante de risco**
> (Dupla Ameaça / Frequente / Severa / Baixo Risco); (c) **ano** (recorte temporal).

---

## 2. Importar os dados no Power BI

### Opção A — Excel (recomendada, à prova de localidade)

1. Abra o **Power BI Desktop** → **Página Inicial** → **Obter dados** → **Pasta de Trabalho do Excel**.
2. Selecione `PowerBI/dashboard_powerbi.xlsx`.
3. No **Navegador**, marque **todas as planilhas** (a caixa no topo) → **Carregar**.
4. Pronto: cada aba vira uma tabela com os tipos numéricos já corretos.

### Opção B — CSV

1. **Obter dados** → **Texto/CSV** → selecione um arquivo da pasta `dados/` (repita para cada um),
   **ou** **Obter dados** → **Pasta** → aponte para `PowerBI/dados` e combine.
2. ⚠️ **Separador decimal:** os CSVs usam **ponto** como decimal (padrão internacional). Se o Power BI
   estiver em português e algum número aparecer errado (ex.: `5.53` virar `553`), faça assim:
   **Transformar dados** → selecione a coluna → **Tipo de Dados** → **Usando Localidade…** →
   *Tipo* = Número Decimal, *Localidade* = **Inglês (Estados Unidos)** → OK.
   (Com a Opção A/Excel isso nunca acontece — por isso ela é a recomendada.)

### Conferência rápida pós-importação
Confira em **Exibição de Tabela** se `kpis_gerais` tem 1 linha e `cvss_medio_global = 6,88`.

---

## 3. Modelo de dados (relacionamentos)

As tabelas de fatos **já trazem o nome da CWE embutido** (`cwe_nome_curto`, `cwe_label`), então
você consegue montar quase tudo **sem criar relacionamento nenhum**. Para filtros cruzados entre
páginas/visuais por CWE, crie relacionamentos opcionais a partir da dimensão:

- Em **Modelo**, ligue `dim_cwe[cwe_id]` → `cwe_id` das tabelas:
  `rq1_frequencia_cwe`, `rq1_tendencia_temporal`, `rq2_severidade_cwe`,
  `rq3_dispersao_quadrante`, `rq3_score_risco`, `rq3_overlap_top10`, `lista_priorizacao`
  (cardinalidade **1 : muitos**, direção do filtro **única**, de `dim_cwe` para os fatos).
- As tabelas `kpis_gerais`, `caracterizacao_*`, `distribuicao_cvss`, `rq1_curva_lorenz`,
  `rq1_persistencia_ranking`, `rq3_quadrantes_resumo`, `gqm_perguntas_metricas` e
  `estatisticas_testes` ficam **isoladas** (não precisam de relação).

> Dica: marque `dim_cwe` como **tabela de dimensão** e oculte as colunas `cwe_id` técnicas dos
> fatos, deixando visível só `cwe_label`/`cwe_nome_curto` para os rótulos.

---

## 4. Medidas DAX úteis (opcional, mas deixa o dashboard mais limpo)

Crie uma tabela de medidas (**Inserir dados** → tabela vazia "Medidas") e adicione:

```DAX
Total CVEs distintos = MAX( kpis_gerais[total_cves_distintos] )
Total Relações CVE-CWE = MAX( kpis_gerais[total_relacoes_cve_cwe] )
Total CWEs = MAX( kpis_gerais[total_cwes_distintas] )
Cobertura CVSS % = MAX( kpis_gerais[cobertura_cvss_pct] )
CVSS Médio Global = MAX( kpis_gerais[cvss_medio_global] )
Gini = MAX( kpis_gerais[gini_frequencia] )
CWEs Dupla Ameaça = MAX( kpis_gerais[cwes_dupla_ameaca] )
CWEs Pareto 80% = MAX( kpis_gerais[cwes_pareto_80pct] )
Período = MAX(kpis_gerais[ano_inicio]) & " – " & MAX(kpis_gerais[ano_fim])
```

> Se preferir não usar DAX: ao arrastar uma coluna de `kpis_gerais` para um **Cartão**, o Power BI
> mostra "Soma de…"; como a tabela tem **1 linha só**, a soma é o próprio valor. Funciona igual.

---

## 5. PÁGINA 1 — Caracterização do Dataset  *(Lab04S01)*

**Objetivo da página:** descrever o dataset como um todo **e** seus subgrupos. Título da página:
**"Caracterização do Dataset — Vulnerabilidades NVD (1999–2025)"**.

> Antes de começar: **Exibição** → **Tamanho da Página** → 16:9; defina um tema de cores
> (Seção 9). Use uma faixa de **Cartões** no topo e os gráficos abaixo.

### 5.1 Faixa de KPIs (Cartões) — o todo em números
- **Visual:** 6–7 **Cartões** (ou *multi-card*). **Tabela:** `kpis_gerais`.
- **Campos (um por cartão):** `total_cves_distintos`, `total_relacoes_cve_cwe`,
  `total_cwes_distintas`, `cobertura_cvss_pct`, `cvss_medio_global`, `gini_frequencia`,
  e um cartão de **Período** (`ano_inicio`–`ano_fim`).
- **Rótulos sugeridos:** "CVEs distintos", "Relações CVE↔CWE", "CWEs distintas",
  "Cobertura CVSS (%)", "CVSS médio (0–10)", "Índice de Gini".

### 5.2 Volume de CVEs por ano — crescimento da base
- **Visual:** **Gráfico de colunas agrupadas**. **Tabela:** `caracterizacao_por_ano`.
- **Eixo X:** `ano` · **Eixo Y (valores):** `cves_distintos`.
- **Título:** "Volume de CVEs por ano (1999–2025)". **Rótulo do eixo Y:** "Nº de CVEs distintos".
- **Leitura:** salta de ~3 mil (2011) para ~45 mil (2025) — justifica o problema de "fadiga de alertas".
- *(Opcional)* adicione uma **linha** com `cvss_media` num eixo secundário (vire **Gráfico de colunas e linhas**).

### 5.3 Distribuição por severidade — **SUBGRUPO** (todo o período)
- **Visual:** **Rosca (Donut)** *ou* **Barras**. **Tabela:** `caracterizacao_por_severidade`.
- **Legenda/Eixo:** `severidade` · **Valores:** `n_cves`.
- **Ordenar** por `ordem` (LOW→CRITICAL): clique ⋯ no visual → *Classificar eixo* → `ordem`.
- **Título:** "CVEs por classe de severidade". Mostre o **%** nos rótulos de dados.
- **Cores:** CRITICAL = vermelho escuro, HIGH = laranja, MEDIUM = amarelo, LOW = verde (ver Seção 9).
- **Acompanhe com uma Tabela** (mesma fonte) com colunas `severidade`, `n_cves`, `pct`,
  `cvss_media`, `cvss_mediana` → caracteriza cada subgrupo com a **medida de tendência central** correta.

### 5.4 Severidade ao longo do tempo — **SUBGRUPO × tempo**
- **Visual:** **Gráfico de colunas 100% empilhadas**. **Tabela:** `caracterizacao_severidade_por_ano`.
- **Eixo X:** `ano` · **Valores:** `n_cves` · **Legenda:** `severidade`.
- **Título:** "Composição de severidade por ano (%)". Ordene a legenda por `ordem`.
- **Leitura:** a partir de 2016 cresce a fatia HIGH/CRITICAL (entrada do CVSS v3).

### 5.5 Distribuição do CVSS — forma da variável-chave
- **Visual:** **Gráfico de colunas**. **Tabela:** `distribuicao_cvss`.
- **Eixo X:** `cvss_score_arredondado` · **Valores:** `n_cves` · **Legenda:** `faixa_severidade`.
- **Título:** "Distribuição do score CVSS". **Rótulo eixo X:** "Score CVSS (0–10)".
- **Leitura:** distribuição **multimodal** (picos em ~5,5; ~7,5; ~9,8).
- **Caixa de texto** ao lado: "Tendência central do CVSS: média 6,88 · mediana 7,00 · moda 7,5 ·
  desvio-padrão 1,83 (IQR 2,60)." *(a mediana é a medida mais adequada por ser robusta a outliers.)*

### 5.6 Caixa de texto — fonte e definição do dataset
Adicione uma **Caixa de Texto**: "Fonte: NIST National Vulnerability Database (NVD), coleta
1999–2025. Unidade de análise: relação CVE↔CWE. Cada CVE pode ter mais de uma CWE; por isso o nº
de relações (278.912) é maior que o de CVEs distintos (253.802)."

---

## 6. PÁGINA 2 — RQ1: Frequência  *(Lab04S02)*

**Caixa de texto no topo (pergunta):**
> **RQ1 — Quais CWEs concentram a maior parte das vulnerabilidades?**
> *Hipótese: a distribuição é não-uniforme (princípio de Pareto).*

### 6.1 Pareto — Top 30 CWEs + % acumulada *(M1 + M2)*
- **Visual:** **Gráfico de colunas e linhas empilhadas/agrupadas**. **Tabela:** `rq1_frequencia_cwe`.
- **Eixo X compartilhado:** `cwe_label` · **Colunas (eixo Y):** `cves_count` ·
  **Linha (eixo Y secundário):** `pct_acumulado`.
- **Filtro do visual:** `top30` = `True` (mostra só as 30 mais frequentes).
- **Ordenar o eixo X por `rank_freq`** (crescente): selecione a coluna `cwe_label` no painel de
  dados → **Ferramentas de Coluna** → **Classificar por Coluna** → `rank_freq`.
- **Título:** "Pareto de frequência: poucas CWEs concentram quase tudo".
  **Eixo Y esq.:** "Nº de CVEs"; **Eixo Y dir.:** "% acumulada".
- **Leitura/destaque:** **38 CWEs cobrem 80%** das 278.912 relações. CWE-79 (XSS) sozinha tem 41.171.

### 6.2 Tendência temporal — Top 10 *(M7)*
- **Visual:** **Gráfico de linhas**. **Tabela:** `rq1_tendencia_temporal`.
- **Eixo X:** `ano` · **Valores:** `cves_count` · **Legenda:** `cwe_nome_curto`.
- **Filtro do visual:** `is_top10` = `True`.
- **Título:** "Evolução anual das 10 CWEs mais frequentes". **Eixo Y:** "Nº de CVEs no ano".
- **Leitura:** XSS dispara após 2017; Buffer Overflow (CWE-119) cai após 2018; Missing Authorization
  (CWE-862) emerge a partir de 2018.

### 6.3 Curva de Lorenz / desigualdade *(M9)*
- **Visual:** **Gráfico de área** ou **linhas**. **Tabela:** `rq1_curva_lorenz`.
- **Eixo X:** `pct_cwes_acumulado` · **Valores:** `pct_cves_acumulado` **e** `linha_igualdade`.
- **Título:** "Curva de Lorenz — concentração de CVEs (Gini = 0,92)".
  **Eixos:** X = "% acumulado de CWEs", Y = "% acumulado de CVEs".
- **Leitura:** quanto mais a curva azul afunda em relação à diagonal, maior a desigualdade.

### 6.4 Persistência do ranking *(M10)* — *opcional*
- **Visual:** **Gráfico de linhas**. **Tabela:** `rq1_persistencia_ranking`.
- **Eixo X:** `transicao` (ou `ano_atual`) · **Valores:** `jaccard`.
- **Título:** "Estabilidade do Top-20 entre anos (índice de Jaccard)". Y de 0 a 1.
- **Leitura:** Jaccard ~0,7 → o ranking muda devagar (CWEs dominantes persistem).

### 6.5 Cartões de apoio
`kpis_gerais`: **CWEs Pareto 80%** (38) e **Gini** (0,92).

---

## 7. PÁGINA 3 — RQ2: Severidade  *(Lab04S02)*

**Caixa de texto no topo (pergunta):**
> **RQ2 — Quais CWEs têm maior potencial técnico de dano (CVSS)?**
> *Hipótese: a severidade varia significativamente entre as CWEs.*

### 7.1 Top CWEs por CVSS médio *(M3)*
- **Visual:** **Gráfico de barras**. **Tabela:** `rq2_severidade_cwe`.
- **Eixo Y:** `cwe_label` · **Valores:** `cvss_medio` · **Cor:** `faixa_cvss_medio`.
- **Filtro:** `top30_cvss` = `True` (Top 30 por severidade). Ordene por `cvss_medio` (desc).
- **Título:** "Top 30 CWEs por CVSS médio". **Eixo de valores:** "CVSS médio (0–10)".
- ⚠️ **Tendência central:** o **CVSS médio** é a medida-chave; mantenha o rótulo do eixo explícito.
  Há um cluster de ~15 CWEs com CVSS ≈ 9,8.
- *(Opcional)* adicione um **filtro de nível de visual** `cves_count ≥ 30` para não destacar CWEs
  raras (poucas CVEs) que ficam em 100% por acaso.

### 7.2 Top CWEs por % HIGH/CRITICAL *(M4)*
- **Visual:** **Gráfico de barras**. **Tabela:** `rq2_severidade_cwe`.
- **Eixo Y:** `cwe_label` · **Valores:** `high_critical_pct`. Ordene desc; mostre Top 20–30.
- **Título:** "% de CVEs HIGH/CRITICAL por CWE". **Eixo:** "% HIGH/CRITICAL".
- **Leitura:** complementa o CVSS médio mostrando consistência (não é efeito de outlier).

### 7.3 Severidade do dataset (referência)
Reutilize `caracterizacao_por_severidade` (rosca + tabela) como **linha de base** para comparar
cada CWE com a média global (CVSS médio global = 6,88).

### 7.4 Caixa de texto — veredito
> "Kruskal-Wallis: p < 0,001 → a severidade **varia significativamente** entre as CWEs. Existem
> CWEs raras, porém quase sempre CRITICAL (CVSS ≈ 9,8)."

---

## 8. PÁGINA 4 — RQ3: Interação Frequência × Severidade

**Caixa de texto no topo (pergunta):**
> **RQ3 — Ser frequente é o mesmo que ser perigoso?**
> *Hipótese: o risco é "bimodal" — as dimensões são independentes no geral, mas há um subgrupo
> crítico que combina as duas.*

### 8.1 Dispersão Frequência × Severidade + Quadrantes *(M6 + M8)*
- **Visual:** **Gráfico de dispersão**. **Tabela:** `rq3_dispersao_quadrante`.
- **Eixo X:** `cves_count` · **Eixo Y:** `cvss_medio` · **Legenda (cor):** `quadrante` ·
  **Detalhes:** `cwe_label` (e `cwe_id` em *Detalhes* para o tooltip).
- **Eixo X logarítmico:** formate o eixo X → **Escala** → **Logarítmica** (a frequência é muito assimétrica).
- **Título:** "Frequência × Severidade por CWE — quadrantes de risco".
  **Eixos:** X = "Nº de CVEs (escala log)", Y = "CVSS médio".
- **Leitura:** a nuvem é praticamente plana (**Spearman ρ ≈ −0,02, p = 0,62 → independência**), mas o
  canto superior-direito (vermelho, **Dupla Ameaça**) reúne 53 CWEs que são frequentes **e** severas.
- *(Opcional)* linhas de referência: **Analítica** → Linha constante X = 66 (Q3 freq) e Y = 7,52 (Q3 CVSS).

### 8.2 Score de risco composto — Top 30 *(M5)*
- **Visual:** **Gráfico de barras**. **Tabela:** `rq3_score_risco`.
- **Eixo Y:** `cwe_label` · **Valores:** `score_risco` · **Cor:** `faixa_risco`. Ordene por `score_risco` desc.
- **Título:** "Score de risco composto (frequência × severidade)". **Eixo:** "Score (0–1)".
- **Leitura:** só **CWE-79** passa de 0,5; **CWE-89** e **CWE-787** vêm logo atrás — o "Pareto do risco".

### 8.3 Resumo dos quadrantes *(M8)*
- **Visual:** **Rosca** ou **Barras**. **Tabela:** `rq3_quadrantes_resumo`.
- **Legenda/Eixo:** `quadrante` · **Valores:** `n_cwes`. Ordene por `ordem`. Mostre `pct` no rótulo.
- **Título:** "Distribuição das 749 CWEs por quadrante de risco".
- **Leitura:** Dupla Ameaça 53 (7,1%) · Severa 135 · Frequente 137 · Baixo Risco 424.

### 8.4 Sobreposição dos Top-10 *(M11)* — *opcional*
- **Visual:** **Matriz**. **Tabela:** `rq3_overlap_top10`.
- **Linhas:** `cwe_label` · **Colunas:** `criterio` · **Valores:** `presente` (Máximo).
- **Formatação condicional** em `presente` (1 = preenchido). **Título:** "Top-10: Frequência vs.
  Severidade vs. Risco". **Leitura:** os Top-10 de frequência e de severidade **não se sobrepõem** (Jaccard ≈ 0).

### 8.5 Fila de priorização — a entrega prática
- **Visual:** **Tabela**. **Tabela:** `lista_priorizacao` (as 53 CWEs do quadrante Dupla Ameaça).
- **Colunas:** `prioridade`, `cwe_id`, `cwe_nome_curto`, `cves_count`, `cvss_medio`,
  `high_critical_pct`, `score_risco`. Ordene por `prioridade`.
- **Título:** "Fila de priorização estratégica (quadrante Dupla Ameaça)".
- É o coração do trabalho: a lista do que **um time de segurança deve tratar primeiro**.

### 8.6 Cartões de apoio
`kpis_gerais`: **Spearman ρ** (−0,018), **CWEs Dupla Ameaça** (53).

---

## 9. Storytelling e identidade visual

### 9.1 Página/seção de síntese (auto-explicativa)
Crie uma **Tabela** com `gqm_perguntas_metricas` (colunas `pergunta`, `titulo`, `hipotese`,
`metricas`, `veredito`, `evidencia`) e outra com `estatisticas_testes`. Elas amarram a "história":
pergunta → métrica → veredito. Útil como rodapé de cada página de RQ ou como página de conclusão.

### 9.2 Paleta de cores sugerida
- **Severidade:** CRITICAL `#7E0023` · HIGH `#E8702A` · MEDIUM `#F2C037` · LOW `#3B8C3F`.
- **Quadrantes:** Dupla Ameaça `#C0392B` · Severa `#E8702A` · Frequente `#2E86C1` · Baixo Risco `#95A5A6`.
- **Faixa de risco (M5):** Crítico `#C0392B` · Alto `#E8702A` · Moderado/Baixo `#2E86C1`.
- Defina em **Exibição → Temas → Personalizar tema atual**, e nas cores de cada visual use a coluna
  de categoria (`severidade`, `quadrante`, `faixa_risco`) para o mapeamento ficar consistente.

### 9.3 Boas práticas exigidas pelo lab
- **Títulos claros** em todo visual e **rótulos de eixo** que digam a **medida** (ex.: "CVSS médio (0–10)").
- Escolha consciente da **tendência central**: use **mediana** para o CVSS global (robusta) e
  **média** quando comparar CWEs pelo CVSS médio (M3). Deixe isso explícito no rótulo/legenda.
- Cada página de RQ deve **mostrar a pergunta** (caixa de texto) **antes** dos gráficos.

---

## 10. Exportar o dashboard em PDF (entrega)

1. Ajuste cada página (16:9, **Exibição → Ajustar à Página**).
2. **Arquivo → Exportar → Exportar para PDF** (gera 1 página de PDF por página do relatório).
   - Alternativa: **Arquivo → Imprimir** → "Microsoft Print to PDF".
3. Salve como, por exemplo, `Dashboard_Priorizacao_CWEs.pdf` e entregue junto com o `.pbix`.

---

## 11. Publicar o dashboard online (acessar de qualquer lugar)

Como os dados são **públicos** (base NIST NVD), dá para deixar o dashboard acessível por um
**link aberto, sem login**, usando o recurso **"Publicar na Web (público)"** do Power BI Service.

### 11.1 Pré-requisitos
- O **`.pbix` pronto** no Power BI Desktop.
- **Conta Microsoft corporativa/escolar** para o `app.powerbi.com` (e-mail Gmail/pessoal **não** serve para login do Power BI).
- **Licença:** a gratuita publica só na sua "Minha área de trabalho" (uso pessoal); para gerar o
  **link público** normalmente é preciso **Pro**.
  > 💡 Muitas universidades dão **Power BI Pro grátis** via Microsoft 365 Education. Faça login no
  > `app.powerbi.com` com o e-mail da faculdade e veja se aparece "Pro" no seu perfil.
- O **administrador do tenant** precisa ter a opção *"Publicar na Web"* habilitada (tenants de
  faculdade/empresa às vezes bloqueiam — vale testar).
- ⚠️ **Sem segurança:** qualquer pessoa com o link vê o relatório e os dados por trás. Como a base
  NVD é pública, tudo bem — mas **não** use este recurso para dados confidenciais.

### 11.2 Passo 1 — Publicar no Power BI Service
1. No **Power BI Desktop**, salve o `.pbix`.
2. **Página Inicial → Publicar** → escolha **"Minha área de trabalho"** → **Publicar**.
3. Acesse `https://app.powerbi.com`, faça login com a **mesma conta** e abra o relatório em
   **Minha área de trabalho → Relatórios**.

### 11.3 Passo 2 — Gerar o link público
1. Com o relatório aberto no Service, clique em **Arquivo** (ou no menu **⋯**) →
   **Inserir relatório → Publicar na Web (público)**.
2. Clique em **Criar código de inserção** → leia o aviso → **Publicar**.
3. Copie:
   - o **Link** → abre o dashboard direto no navegador, de qualquer lugar, **sem login**;
   - ou o **código `<iframe>`** → para embutir o dashboard em um site (GitHub Pages, blog, etc.).

### 11.4 Gerenciar, atualizar e remover
- Para ver/excluir o link: **Configurações ⚙ → Gerenciar códigos de inserção**.
- Após **republicar** alterações grandes, confira o link (às vezes muda) e aguarde alguns minutos
  (o conteúdo público fica em cache e pode levar até ~1 h para refletir mudanças).
- Os dados são um **snapshot** (CSV/Excel), então **não precisa de gateway**. Se atualizar os
  arquivos, **republique** pelo Desktop (ou conecte via OneDrive/SharePoint para atualização automática).

### 11.5 Não tem Pro ou o tenant bloqueou?
- **Power BI Mobile:** instale o app e veja o relatório logado na sua conta (acesso pessoal de qualquer lugar).
- **Looker Studio (grátis):** monte o dashboard no Google Looker Studio e compartilhe por link
  público — veja o guia paralelo **[`GUIA_DASHBOARD_LOOKER_STUDIO.md`](GUIA_DASHBOARD_LOOKER_STUDIO.md)**.
- **PDF hospedado:** suba o PDF exportado (Seção 10) no Google Drive/OneDrive/GitHub e compartilhe o link (estático).

---

## 12. Inserir os gráficos no artigo de TI6

| Gráfico | Seção sugerida do artigo |
|---|---|
| KPIs, volume por ano, severidade (geral e por ano), distribuição CVSS | **3. Metodologia** (caracterização do dataset) |
| Pareto, tendência temporal, Lorenz/Gini, persistência | **4. Resultados — RQ1** |
| Top CVSS médio, % HIGH/CRITICAL | **4. Resultados — RQ2** |
| Dispersão+quadrante, score de risco, resumo de quadrantes, overlap, fila de priorização | **4. Resultados — RQ3** |

> Cada figura deve ser **citada e explicada** no texto (ex.: "A Figura X mostra que 38 CWEs
> concentram 80% das vulnerabilidades, confirmando o padrão de Pareto…").

---

## 13. Checklist final

- [ ] Dados importados (Excel ou CSV) e tipos numéricos corretos (`cvss_medio_global = 6,88`).
- [ ] **Página 1 — Caracterização:** KPIs + volume/ano + severidade (geral) + severidade/ano + CVSS, **com subgrupos**.
- [ ] **Página 2 — RQ1:** pergunta no topo + Pareto + tendência + Lorenz.
- [ ] **Página 3 — RQ2:** pergunta no topo + CVSS médio + % HIGH/CRITICAL.
- [ ] **Página 4 — RQ3:** pergunta no topo + dispersão/quadrante + score + quadrantes + fila de priorização.
- [ ] Todos os visuais com **título** e **rótulos de eixo** claros.
- [ ] Página/tabela de **síntese GQM** (auto-explicativa).
- [ ] Dashboard **exportado em PDF**.
- [ ] *(Opcional)* Dashboard **publicado online** (link "Publicar na Web" ou Looker Studio).
- [ ] Gráficos inseridos e explicados no **artigo de TI6** (Seções 3 e 4).

---

## 14. Reproduzir/atualizar os dados (se o pipeline rodar de novo)

Os arquivos em `dados/` foram gerados a partir de `codigo/data/` pelo script desta pasta:

```bash
cd PowerBI
python gerar_dados_powerbi.py
```

Ele lê os artefatos do pipeline (`ranking_cwes_priorizados.parquet`, `dados_brutos_cves.parquet`,
`m7_…`, `m8_…`, `m9_…`, `m10_…`, `m11_…`) e regrava os CSVs e o `dashboard_powerbi.xlsx`.
No Power BI, depois é só **Página Inicial → Atualizar**.
