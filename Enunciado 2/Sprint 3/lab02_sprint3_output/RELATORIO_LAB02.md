# Laboratório 02 — Qualidade de sistemas Java (CK + GitHub)

## Mapeamento do enunciado (checklist)

| Pedido no LAB 02 | Onde está coberto |
|------------------|-------------------|
| Top repositórios Java + dados GitHub | Sprint 1 → `repos_java_1000.csv`; consolidado Sprint 2 |
| Automação clone + métricas CK (CBO, DIT, LCOM) | Sprint 1: `--coleta-ck`, `--coleta-lote` |
| CSV com medições (1000 repos) | Sprint 2 → `lab02_medicoes_consolidado.csv` |
| Hipóteses informais | Secção 1 abaixo + PDF |
| Metodologia | Secção 2 |
| Resultados por RQ + sumarização | Secções 3–3.2, `resumo_global_metricas.csv`, `rq0*_*.csv` |
| Discussão e conclusões | Secções 4–5 |
| **Bônus:** gráficos + Pearson/Spearman | Secção 6; pasta `bonus/`; PDF secção 5–7 |
| Entregáveis listados | Secção 8 abaixo |
| Apresentação na aula de entrega | Preparação humana (slides) — não gerada por código |

## 1. Introdução e hipóteses (informais)

- **RQ01 (popularidade × qualidade):** hipótese — repositórios muito populares tendem a acumular código legado e integrações, podendo elevar acoplamento (CBO) e reduzir coesão (LCOM); por outro lado, projetos maduros podem investir em refatoração. Esperamos correlações fracas ou moderadas, não determinísticas.
- **RQ02 (maturidade × qualidade):** hipótese — maior idade correlaciona-se com hierarquias mais profundas (DIT) e mais acoplamento histórico, mas também com mais oportunidades de saneamento.
- **RQ03 (atividade × qualidade):** hipótese — mais *releases* sugere manutenção ativa, possivelmente associada a melhor organização modular (menos LCOM extremo), embora ritmo alto também possa introduzir dívidas técnicas.
- **RQ04 (tamanho × qualidade):** hipótese — maior LOC costuma acompanhar mais classes e dependências, elevando CBO; LCOM pode subir em módulos mal particionados.

## 2. Metodologia

- Lista dos repositórios Java mais populares (GitHub) e métricas de processo: estrelas, idade (anos), releases, tamanho (LOC Java e linhas de comentário quando disponíveis via CK), `disk_usage_kb` quando presente na API.
- Métricas de qualidade (CK): CBO, DIT e LCOM, sumarizadas **por repositório** (média, mediana e desvio entre classes), conforme orientação do enunciado.
- Análise: estatísticas descritivas globais; estratificação por quartis das métricas de processo; correlação de Pearson e Spearman (bilateral) com p-valor aproximado via estatística *t* com *n−2* graus de liberdade (uso comum em laboratórios).
- **Ficheiro consolidado:** `lab02_medicoes_consolidado.csv` (caminho absoluto na pasta Sprint 2 após `dotnet run` na Sprint 2).
- **Amostra deste relatório:** *n* = 990 repositórios na lista consolidada; repositórios com CK preenchido: **962** (para correlações e quartis de qualidade, exige CK por repositório).

## 3. Resultados — processo (todos os repositórios da lista)

| Métrica | n | Média | Mediana | Desvio padrão |
|---------|---|-------|---------|---------------|
| Estrelas | 990 | 9873,17 | 5935,00 | 12015,81 |
| Forks | 990 | 2460,27 | 1404,00 | 4009,59 |
| Idade (anos) | 990 | 10,14 | 10,31 | 3,18 |
| Releases | 990 | 41,57 | 11,50 | 89,90 |
| Disco (KB, API) | 0 | — | — | — |
| LOC Java (onde medido) | 962 | 117597,43 | 18269,50 | 423611,57 |
| Linhas de comentário (CK) | 962 | 42945,96 | 4847,50 | 408037,03 |

### 3.0 Correlações exploratórias (somente métricas de processo)

Relação entre variáveis de processo (Pearson / Spearman, *p* bilateral aproximado):

- **Estrelas × Idade (anos)** (*n* = 990): Pearson *r* = -0,0294 (*p* ≈ 3,561E-001); Spearman ρ = 0,0370 (*p* ≈ 2,448E-001).
- **Estrelas × Releases** (*n* = 990): Pearson *r* = 0,1090 (*p* ≈ 5,894E-004); Spearman ρ = 0,1316 (*p* ≈ 3,265E-005).
- **Idade (anos) × Releases** (*n* = 990): Pearson *r* = 0,0273 (*p* ≈ 3,903E-001); Spearman ρ = 0,0109 (*p* ≈ 7,314E-001).

## 3.1 Qualidade (CK) — repositórios com medição

| CBO (média por repo) | 962 | 5,37 | 5,34 | 1,87 |
| DIT (média por repo) | 962 | 1,46 | 1,39 | 0,37 |
| LCOM (média por repo) | 962 | 118,14 | 24,51 | 1762,66 |


## 3.2 Resultados por questão de pesquisa (quartis de processo × médias CK)

Tabelas geradas a partir dos CSV `rq01`–`rq04` (colunas vazias ou *—* quando `n_com_ck = 0`).

#### RQ01 — popularidade (estrelas)

| RQ01_popularidade_estrelas | quartil_processo | n_com_ck | cbo_media_media | cbo_media_mediana | dit_media_media | lcom_media_media |
|---|---|---|---|---|---|---|
| RQ01_popularidade_estrelas | Q1_baixo | 243 | 5.3494 | 5.2596 | 1.4623 | 43.6314 |
| RQ01_popularidade_estrelas | Q2 | 247 | 5.3008 | 5.1929 | 1.4652 | 54.7078 |
| RQ01_popularidade_estrelas | Q3 | 241 | 5.5900 | 5.5871 | 1.4684 | 65.8930 |
| RQ01_popularidade_estrelas | Q4_alto | 231 | 5.2444 | 5.3434 | 1.4297 | 318.8606 |

#### RQ02 — maturidade (idade)

| RQ02_maturidade_idade | quartil_processo | n_com_ck | cbo_media_media | cbo_media_mediana | dit_media_media | lcom_media_media |
|---|---|---|---|---|---|---|
| RQ02_maturidade_idade | Q1_baixo | 237 | 5.4661 | 5.5400 | 1.3663 | 61.8672 |
| RQ02_maturidade_idade | Q2 | 244 | 5.1923 | 5.1500 | 1.4255 | 51.9709 |
| RQ02_maturidade_idade | Q3 | 241 | 5.3824 | 5.2543 | 1.4937 | 278.1094 |
| RQ02_maturidade_idade | Q4_alto | 240 | 5.4513 | 5.3978 | 1.5408 | 80.3520 |

#### RQ03 — atividade (releases)

| RQ03_atividade_releases | quartil_processo | n_com_ck | cbo_media_media | cbo_media_mediana | dit_media_media | lcom_media_media |
|---|---|---|---|---|---|---|
| RQ03_atividade_releases | Q1_baixo | 304 | 4.5006 | 4.5903 | 1.3802 | 217.7377 |
| RQ03_atividade_releases | Q2 | 193 | 5.1821 | 5.0000 | 1.4636 | 61.6226 |
| RQ03_atividade_releases | Q3 | 234 | 5.6959 | 5.6441 | 1.4930 | 75.4640 |
| RQ03_atividade_releases | Q4_alto | 231 | 6.3493 | 6.1705 | 1.5150 | 77.5250 |

#### RQ04 — tamanho (LOC Java)

| RQ04_tamanho_loc_java | quartil_processo | n_com_ck | cbo_media_media | cbo_media_mediana | dit_media_media | lcom_media_media |
|---|---|---|---|---|---|---|
| RQ04_tamanho_loc_java | Q1_baixo | 241 | 4.5095 | 4.6000 | 1.3583 | 30.0772 |
| RQ04_tamanho_loc_java | Q2 | 241 | 4.9370 | 5.0000 | 1.4195 | 35.3280 |
| RQ04_tamanho_loc_java | Q3 | 240 | 5.6465 | 5.5884 | 1.5238 | 80.2837 |
| RQ04_tamanho_loc_java | Q4_alto | 240 | 6.4003 | 6.3523 | 1.5260 | 327.5897 |

### 3.3 Correlações (ficheiros CSV em `bonus/`)

#### Processo × processo

| x | y | pearson_r | pearson_p_aprox | spearman_rho | spearman_p_aprox | n |
|---|---|---|---|---|---|---|
| estrelas | idade_anos | -0.0294 | 3.561E-001 | 0.0370 | 2.448E-001 | 990 |
| estrelas | releases | 0.1090 | 5.894E-004 | 0.1316 | 3.265E-005 | 990 |
| idade_anos | releases | 0.0273 | 3.903E-001 | 0.0109 | 7.314E-001 | 990 |

#### Processo × qualidade (CK)

| x | y | pearson_r | pearson_p_aprox | spearman_rho | spearman_p_aprox | n |
|---|---|---|---|---|---|---|
| estrelas | cbo_media | -0.1318 | 4.138E-005 | 0.0208 | 5.193E-001 | 962 |
| estrelas | dit_media | -0.1095 | 6.725E-004 | -0.0485 | 1.326E-001 | 962 |
| estrelas | lcom_media | 0.0179 | 5.796E-001 | 0.0510 | 1.138E-001 | 962 |
| idade_anos | cbo_media | 0.0046 | 8.878E-001 | 0.0003 | 9.924E-001 | 962 |
| idade_anos | dit_media | 0.1683 | 1.516E-007 | 0.2798 | 0.000E+000 | 962 |
| idade_anos | lcom_media | 0.0268 | 4.059E-001 | 0.1947 | 1.139E-009 | 962 |
| releases | cbo_media | 0.2144 | 1.812E-011 | 0.4041 | 0.000E+000 | 962 |
| releases | dit_media | 0.0507 | 1.163E-001 | 0.2099 | 4.838E-011 | 962 |
| releases | lcom_media | -0.0126 | 6.974E-001 | 0.3286 | 0.000E+000 | 962 |
| loc_java | cbo_media | 0.1943 | 1.237E-009 | 0.4237 | 0.000E+000 | 962 |
| loc_java | dit_media | 0.0282 | 3.817E-001 | 0.2340 | 1.965E-013 | 962 |
| loc_java | lcom_media | 0.0531 | 1.001E-001 | 0.4542 | 0.000E+000 | 962 |

## 4. Discussão (hipóteses × dados observados)

- Com **apenas métricas de processo**, observamos a distribuição de popularidade, idade e *releases* no *top* Java; ela é fortemente assimétrica (poucos repositórios concentram muitas estrelas).
- Com **CK disponível para vários repositórios**, as tabelas por quartil e os gráficos em `lab02_sprint3_output/bonus/` permitem confrontar se faixas de popularidade/maturidade/atividade/tamanho acompanham diferenças nas médias de CBO/DIT/LCOM.
- **Limitações:** métricas CK dependem do escaneamento do CK na árvore de fontes; comparações entre repositórios com estruturas diferentes exigem cautela. *Releases* podem estar subcontados consoante o endpoint da API usado na Sprint 1.

## 5. Conclusões

- Dispõe-se de CK para **962** de **990** repositórios: as inferências sobre qualidade aplicam-se a este subconjunto (possível viés de seleção se a coleta não for aleatória).
- As tabelas por quartil e as correlações processo×CK permitem uma leitura exploratória das RQs; significado prático exige interpretação no contexto de cada métrica CK.

## 6. Bônus (figuras e testes de correlação)

- Gráficos PNG: pasta `bonus/` (`pdf_*.png` agregados + `scatter_*.png` quando há pares válidos, *n* ≥ 4).
- Relatório final em PDF: `Relatorio_Final_Lab02.pdf` (gerado na mesma pasta que este Markdown).

## 7. Referências

- Aniche, M. **CK** (Chidamber & Kemerer). https://github.com/mauricioaniche/ck
- GitHub REST / GraphQL API. https://docs.github.com/
- MathNet.Numerics (correlações). https://github.com/mathnet/mathnet-numerics

## 8. Entregáveis gerados (pasta `lab02_sprint3_output/`)

- `RELATORIO_LAB02.md` — este relatório.
- `Relatorio_Final_Lab02.pdf` — versão PDF (se não usar `--no-pdf`).
- `resumo_global_metricas.csv` — estatísticas globais.
- `rq01_quartis_estrelas_vs_ck.csv` … `rq04_quartis_loc_vs_ck.csv` — estratificação por quartis.
- `bonus/correlacoes_*.csv`, `bonus/*.png` — bônus.

---
*Gerado automaticamente pela Sprint 3 (`dotnet run`).*
