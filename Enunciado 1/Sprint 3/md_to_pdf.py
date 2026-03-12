from pathlib import Path
from xml.sax.saxutils import escape

from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Preformatted


def markdown_to_pdf(md_path: Path, pdf_path: Path) -> None:
    styles = getSampleStyleSheet()
    normal = styles["BodyText"]
    normal.fontName = "Helvetica"
    normal.fontSize = 10
    normal.leading = 14

    h1 = ParagraphStyle("H1", parent=styles["Heading1"], fontName="Helvetica-Bold", fontSize=18, leading=22, spaceBefore=10, spaceAfter=8)
    h2 = ParagraphStyle("H2", parent=styles["Heading2"], fontName="Helvetica-Bold", fontSize=14, leading=18, spaceBefore=8, spaceAfter=6)
    h3 = ParagraphStyle("H3", parent=styles["Heading3"], fontName="Helvetica-Bold", fontSize=12, leading=16, spaceBefore=6, spaceAfter=4)
    bullet = ParagraphStyle("Bullet", parent=normal, leftIndent=14)
    code = ParagraphStyle("Code", parent=normal, fontName="Courier", fontSize=9, leading=12)

    story = []
    in_code_block = False
    code_lines = []

    for raw in md_path.read_text(encoding="utf-8").splitlines():
        line = raw.rstrip("\n")

        if line.strip().startswith("```"):
            in_code_block = not in_code_block
            if not in_code_block and code_lines:
                story.append(Preformatted("\n".join(code_lines), code))
                story.append(Spacer(1, 0.2 * cm))
                code_lines = []
            continue

        if in_code_block:
            code_lines.append(line)
            continue

        stripped = line.strip()
        if not stripped:
            story.append(Spacer(1, 0.15 * cm))
            continue

        if stripped.startswith("# "):
            story.append(Paragraph(escape(stripped[2:].strip()), h1))
            continue
        if stripped.startswith("## "):
            story.append(Paragraph(escape(stripped[3:].strip()), h2))
            continue
        if stripped.startswith("### "):
            story.append(Paragraph(escape(stripped[4:].strip()), h3))
            continue

        if stripped.startswith("- "):
            story.append(Paragraph(f"• {escape(stripped[2:].strip())}", bullet))
            continue

        if stripped.startswith("|") and stripped.endswith("|"):
            story.append(Preformatted(line, code))
            continue

        story.append(Paragraph(escape(stripped), normal))

    if code_lines:
        story.append(Preformatted("\n".join(code_lines), code))

    doc = SimpleDocTemplate(
        str(pdf_path),
        pagesize=A4,
        leftMargin=1.6 * cm,
        rightMargin=1.6 * cm,
        topMargin=1.6 * cm,
        bottomMargin=1.6 * cm,
        title=md_path.stem,
        author="GitHub Copilot",
    )
    doc.build(story)


if __name__ == "__main__":
    md = Path("Enunciado 1/Sprint 3/Relatorio_Final_Sprint3.md")
    pdf = Path("Enunciado 1/Sprint 3/Relatorio_Final_Sprint3.pdf")
    markdown_to_pdf(md, pdf)
    print(f"PDF gerado em: {pdf}")
