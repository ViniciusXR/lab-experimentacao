# PowerBI — Lab04 / TI6 · Priorização Estratégica de CWEs

Materiais prontos para montar o **dashboard do Laboratório 04** (Power BI) a partir dos dados
do trabalho de TI6 (base NIST NVD, 1999–2025).

## Por onde começar

➡️ **Power BI:** leia o [`GUIA_DASHBOARD_POWERBI.md`](GUIA_DASHBOARD_POWERBI.md) — passo a passo
completo: importação, modelo de dados, **cada visual** das páginas de Caracterização e das
RQ1/RQ2/RQ3, **publicação online** (Seção 11) e exportação em PDF.

➡️ **Looker Studio (100% web, grátis):** leia o
[`GUIA_DASHBOARD_LOOKER_STUDIO.md`](GUIA_DASHBOARD_LOOKER_STUDIO.md) — mesma estrutura, usando os
mesmos CSVs, com compartilhamento por link público sem licença.

## Conteúdo

| Arquivo | O que é |
|---|---|
| `dashboard_powerbi.xlsx` | Todas as tabelas em 1 arquivo (1 aba por tabela). **Importe este no Power BI.** |
| `dados/*.csv` | As mesmas tabelas em CSV (UTF-8, decimal com ponto). Servem para Power BI **e** Looker Studio. |
| `GUIA_DASHBOARD_POWERBI.md` | Guia de construção no Power BI (Lab04S01/S02/S03) + publicação online. |
| `GUIA_DASHBOARD_LOOKER_STUDIO.md` | Guia equivalente no Google Looker Studio (web, grátis, link público). |
| `gerar_dados_powerbi.py` | Script que regenera as tabelas a partir de `../codigo/data/`. |

## Deixar o dashboard online (acessar de qualquer lugar)

- **Power BI:** "Publicar na Web (público)" → link aberto sem login (requer licença Pro e o recurso
  habilitado pelo admin). Passo a passo na **Seção 11** do guia do Power BI.
- **Looker Studio:** Compartilhar → "Qualquer pessoa com o link" → link público **grátis**.
  Passo a passo na **Seção 8** do guia do Looker Studio.

## Resumo dos números (base 1999–2025)

- 253.802 CVEs distintos · 278.912 relações CVE↔CWE · 749 CWEs · cobertura CVSS 99,99%.
- **RQ1:** 38 CWEs cobrem 80% das CVEs (Pareto; Gini 0,92).
- **RQ2:** cluster de ~15 CWEs com CVSS médio ≈ 9,8 (Kruskal-Wallis p < 0,001).
- **RQ3:** Spearman ρ ≈ −0,02 (independência) **e** 53 CWEs no quadrante "Dupla Ameaça".
