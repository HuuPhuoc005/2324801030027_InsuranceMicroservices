"""Assemble actual browser screenshots and verified runtime results into the submission PDF."""
from pathlib import Path
import json
import re
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Image, PageBreak, Table, TableStyle, Preformatted
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from PIL import Image as PILImage

ROOT = Path(__file__).resolve().parent.parent
SHOTS = ROOT / '.local/assignment5-evidence'
OUTPUT = ROOT / 'output/pdf/Bai5_MinhChung_Eureka_PricingService.pdf'
OUTPUT.parent.mkdir(parents=True, exist_ok=True)

font_dir = Path('C:/Windows/Fonts')
pdfmetrics.registerFont(TTFont('Arial', str(font_dir/'arial.ttf')))
pdfmetrics.registerFont(TTFont('ArialBold', str(font_dir/'arialbd.ttf')))
pdfmetrics.registerFontFamily('Arial', normal='Arial', bold='ArialBold', italic='Arial', boldItalic='ArialBold')
styles = getSampleStyleSheet()
styles.add(ParagraphStyle(name='TitleVN', fontName='ArialBold', fontSize=19, leading=24, textColor=colors.HexColor('#15334F'), spaceAfter=10))
styles.add(ParagraphStyle(name='HeadingVN', fontName='ArialBold', fontSize=11.5, leading=16, spaceBefore=10, spaceAfter=6, textColor=colors.HexColor('#15334F')))
styles.add(ParagraphStyle(name='BodyVN', fontName='Arial', fontSize=10, leading=15, spaceAfter=7))
styles.add(ParagraphStyle(name='SmallVN', fontName='Arial', fontSize=8.5, leading=12, textColor=colors.HexColor('#475569'), spaceAfter=6))
styles.add(ParagraphStyle(name='CodeVN', fontName='Courier', fontSize=9, leading=13, backColor=colors.HexColor('#F1F5F9'), borderPadding=9, spaceBefore=5, spaceAfter=10))

def read_log(path):
    data = path.read_bytes()
    return data.decode('utf-16' if data.startswith((b'\xff\xfe', b'\xfe\xff')) else 'utf-8-sig')

build = read_log(ROOT/'artifacts/bai5-build.log')
tests = read_log(ROOT/'artifacts/bai5-test.log')
runtime = read_log(ROOT/'artifacts/bai5-eureka-check.log')
result = json.loads((ROOT/'artifacts/bai5-pricing-response.json').read_text(encoding='utf-8-sig'))
assert 'Build succeeded.' in build and '0 Warning(s)' in build and '0 Error(s)' in build
assert 'PRICINGSERVICE registered: UP, localhost:5040' in runtime
assert result['totalPrice'] == 98 and result['coverPrices'] == {'C1':26, 'C2':52, 'C3':20}
counts = re.findall(r'Passed!\s*- Failed:\s*(\d+), Passed:\s*(\d+), Skipped:\s*(\d+), Total:\s*(\d+)', tests)
assert counts and all(int(failed) == 0 and int(skipped) == 0 for failed, passed, skipped, total in counts)
passed = sum(int(passed) for failed, passed, skipped, total in counts)

WIDTH = A4[0] - 72
def p(text, style='BodyVN'):
    return Paragraph(text, styles[style])
def screenshot(name):
    path = SHOTS/name
    with PILImage.open(path) as im:
        w, h = im.size
    return Image(str(path), width=WIDTH, height=WIDTH*h/w)
def footer(canvas, doc):
    canvas.setStrokeColor(colors.HexColor('#CBD5E1'))
    canvas.line(36, 32, A4[0]-36, 32)
    canvas.setFont('Arial', 8)
    canvas.setFillColor(colors.HexColor('#64748B'))
    canvas.drawString(36, 20, 'Bài thực hành 5 • HuuPhuoc005 • 18/10/2026')
    canvas.drawRightString(A4[0]-36, 20, f'Trang {doc.page}')

story = [
    p('BÀI THỰC HÀNH 5', 'SmallVN'),
    p('Minh chứng Eureka và PricingService', 'TitleVN'),
    p('Repository: <link href="https://github.com/HuuPhuoc005/2324801030027_InsuranceMicroservices" color="#2563EB">github.com/HuuPhuoc005/2324801030027_InsuranceMicroservices</link>'),
    p('1. PricingService đăng ký trên Eureka', 'HeadingVN'),
    p('Truy cập <b>http://localhost:8761</b>. Ảnh chụp trình duyệt thực tế bên dưới hiển thị <b>PRICINGSERVICE — UP (1)</b>, instance <b>localhost:PricingService:5040</b>.'),
    screenshot('eureka.jpg'),
    Spacer(1, 8),
    p('Ảnh chụp giữ nguyên thông tin của trang. Ở thời điểm chụp, số lượt renew còn thấp hơn ngưỡng nên Eureka hiện cảnh báo self-preservation. Trạng thái hoạt động đã được kiểm tra chéo bằng HTTP health và API tính phí ở trang 2.', 'SmallVN'),
    PageBreak(),
    p('2. Đối chiếu hoạt động của dịch vụ', 'TitleVN'),
    p('<b>http://localhost:5040/info</b> — thông tin instance mà liên kết trên Eureka trỏ tới.'),
    screenshot('pricing-info.jpg'),
    Spacer(1, 8),
    p('<b>http://localhost:5040/health</b> — dịch vụ phản hồi trạng thái UP.'),
    screenshot('pricing-health.jpg'),
    p('3. Kết quả gọi API và kiểm thử', 'HeadingVN'),
    p('Gọi thực tế <b>POST http://localhost:5040/api/Pricing</b> cho sản phẩm TRI, 1 người lớn, 1 trẻ em, điểm đến EUR, chọn C1/C2/C3. HTTP 200, kết quả lưu trong <b>artifacts/bai5-pricing-response.json</b>:'),
    Preformatted(json.dumps(result, indent=2, ensure_ascii=False), styles['CodeVN']),
]
rows = [
    [p('<b>Kiểm tra</b>'), p('<b>Kết quả</b>')],
    [p('Build toàn solution'), p('30 project, 0 lỗi, 0 cảnh báo')],
    [p('Test toàn solution'), p(f'{passed} pass, 0 fail, 0 skip')],
    [p('PricingService.Test'), p('16 test pass, gồm các test mẫu giảng viên')],
    [p('PricingService.IntegrationTest'), p('5 test HTTP pass với PostgreSQL thật')],
    [p('Yêu cầu tính phí không hợp lệ'), p('HTTP 400')],
]
table = Table(rows, colWidths=[WIDTH*.51, WIDTH*.49])
table.setStyle(TableStyle([
    ('BACKGROUND',(0,0),(-1,0),colors.HexColor('#E2E8F0')),
    ('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.white,colors.HexColor('#F8FAFC')]),
    ('VALIGN',(0,0),(-1,-1),'TOP'),
    ('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),
    ('TOPPADDING',(0,0),(-1,-1),5),('BOTTOMPADDING',(0,0),(-1,-1),2),
    ('LINEBELOW',(0,0),(-1,0),.5,colors.HexColor('#94A3B8')),
]))
story += [table, Spacer(1, 10), p('Môi trường thực chạy: Windows, .NET 8, Java 11, PostgreSQL cục bộ. Integration test tạo và dọn CSDL riêng; không dùng dữ liệu thực hành để chạy test. Chế độ Testcontainers vẫn được giữ trong source, nhưng chưa chạy ở lần kiểm tra này vì Docker engine không hoạt động.', 'SmallVN')]
doc = SimpleDocTemplate(str(OUTPUT), pagesize=A4, rightMargin=36, leftMargin=36, topMargin=32, bottomMargin=44,
    title='Bài thực hành 5 - Minh chứng Eureka và PricingService', author='HuuPhuoc005')
doc.build(story, onFirstPage=footer, onLaterPages=footer)
print(OUTPUT)
