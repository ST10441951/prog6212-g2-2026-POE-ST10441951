"""Check the Part 1 endpoint plan and database documents before building the API.

Sources:
The IIE (2026), POE Part I General Feedback (HE), required operations.
Python Software Foundation (2026), re, pathlib and ElementTree documentation:
https://docs.python.org/3/library/re.html
https://docs.python.org/3/library/pathlib.html
https://docs.python.org/3/library/xml.etree.elementtree.html
Accessed: 17 October 2026.
"""

from pathlib import Path
import html
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]

# These 25 method-and-route pairs match the lecturer's required operations.
REQUIRED = """
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
GET /api/profile
PUT /api/profile
GET /api/events
GET /api/events/{eventId}
GET /api/events/mine
POST /api/events
PUT /api/events/{eventId}
DELETE /api/events/{eventId}
GET /api/events/{eventId}/categories
POST /api/events/{eventId}/categories
PUT /api/events/{eventId}/categories/{categoryId}
DELETE /api/events/{eventId}/categories/{categoryId}
POST /api/enrolments
GET /api/enrolments/mine
GET /api/enrolments/{enrolmentId}
GET /api/events/{eventId}/enrolments
PUT /api/enrolments/{enrolmentId}/cancel
DELETE /api/enrolments/{enrolmentId}
POST /api/enrolments/{enrolmentId}/result
GET /api/results/mine
GET /api/events/{eventId}/results
PUT /api/enrolments/{enrolmentId}/result
""".strip().splitlines()


def check(condition, message):
    if not condition:
        raise ValueError(message)


def check_endpoints():
    plan = (ROOT / 'docs/API-Endpoint-Plan.md').read_text(encoding='utf-8')
    header = '| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |'
    check(header in plan, 'The six-column endpoint header is missing.')
    endpoints = []
    for line in plan.splitlines():
        if not re.match(r'^\| (GET|POST|PUT|DELETE) \|', line):
            continue
        cells = [cell.strip() for cell in line.strip('|').split('|')]
        check(len(cells) == 6 and all(cells), f'Incomplete endpoint row: {line}')
        method, route = cells[:2]
        check(re.fullmatch(r'`/api/[^`\s]+`', route), f'Invalid API route: {route}')
        check(re.search(r'`2\d\d ', cells[5]), f'No success response: {method} {route}')
        endpoints.append(f'{method} {route[1:-1]}')
    check(len(endpoints) == len(set(endpoints)), 'Duplicate method-and-route pairs found.')
    missing = set(REQUIRED) - set(endpoints)
    check(not missing, 'Missing required operations: '+', '.join(sorted(missing)))
    check(len(endpoints) == 40, f'Expected the 40 planned endpoints; found {len(endpoints)}.')

    mapping = re.findall(r'^\| (\d+) \| [^|]+ \| [^|]+ \| (GET|POST|PUT|DELETE) `([^`]+)` \|$', plan, re.M)
    check([int(row[0]) for row in mapping] == list(range(1, 26)), 'The required-operation mapping must list 1 to 25 once each.')
    check([f'{method} {route}' for _, method, route in mapping] == REQUIRED, 'The 25-operation mapping differs from the required methods and routes.')
    print('Passed: 40 unique, complete endpoint rows and all 25 required operations.')


def check_schema_documents():
    sql = (ROOT / 'docs/RaceDay.sql').read_text(encoding='utf-8')
    dictionary = (ROOT / 'docs/RaceDay-Data-Dictionary.md').read_text(encoding='utf-8')
    diagram = ET.parse(ROOT / 'docs/RaceDay-ERD.drawio')
    svg = ET.parse(ROOT / 'docs/RaceDay-ERD.svg')
    check(diagram.getroot().tag == 'mxfile', 'Invalid draw.io root element.')
    check(svg.getroot().tag == '{http://www.w3.org/2000/svg}svg', 'Invalid SVG root element.')
    cells = {cell.attrib['id']: cell for cell in diagram.iter('mxCell')}
    tables = re.findall(r'CREATE TABLE dbo\.(\w+)\s*\((.*?)\n    \);', sql, re.S)
    check(len(tables) == 7, 'Expected seven SQL table definitions.')
    svg_tables = {name: [] for name, _ in tables}
    current_table = None
    for node in svg.iter('{http://www.w3.org/2000/svg}text'):
        text = node.text or ''
        if text in svg_tables:
            current_table = text
        elif current_table is not None:
            svg_tables[current_table].append(text)
    total_columns = 0
    for name, body in tables:
        section = re.search(r'### 5\.\d+ '+re.escape(name)+r'\n(.*?)(?=\n### |\n## |\Z)', dictionary, re.S)
        check(section is not None and name in cells, f'{name} is missing from the dictionary or ERD.')
        dictionary_columns = re.findall(r'^\| `(\w+)` \| `([^`]+)` \| (Yes|No) \|', section.group(1), re.M)
        dictionary_columns = [(col, datatype, 'NULL' if nullable == 'Yes' else 'NOT NULL') for col, datatype, nullable in dictionary_columns]
        columns = re.findall(r'^        (\w+) ((?:INT|TINYINT|NVARCHAR\(\d+\)|DECIMAL\(\d+,\d+\)|DATETIME2\(0\)|DATE|BIT)(?: IDENTITY\(1,1\))?) (NOT NULL|NULL)', body, re.M)
        check(columns == dictionary_columns, f'{name} columns differ between SQL and dictionary.')
        label = html.unescape(cells[name].attrib.get('value', ''))
        label = re.sub(r'<[^>]+>', '\n', label)
        for column, datatype, nullable in columns:
            pattern = re.compile(r'^'+re.escape(column)+r'\s+'+re.escape(datatype)+r'\s+'+re.escape(nullable)+r'(?:\s|$)', re.M)
            check(pattern.search(label), f'{name}.{column} differs in draw.io.')
            check(any(pattern.search(line) for line in svg_tables[name]), f'{name}.{column} differs in SVG.')
        total_columns += len(columns)
    check(total_columns == 71, 'Expected 71 columns across all seven tables.')
    edges = [cell for cell in cells.values() if cell.attrib.get('edge') == '1']
    foreign_keys = [(parent, child) for child, body in tables for parent in re.findall(r'FOREIGN KEY \([^)]+\)\s+REFERENCES dbo\.(\w+)', body)]
    diagram_links = [(cell.attrib.get('source'), cell.attrib.get('target')) for cell in edges]
    check(len(edges) == len(foreign_keys) == 9 and sorted(diagram_links) == sorted(foreign_keys), 'The nine ERD relationships must match the SQL foreign keys.')
    print('Passed: seven tables, 71 matching column definitions and nine relationships.')


def main():
    check_endpoints()
    check_schema_documents()
    print('Planning checks passed. These checks do not execute SQL or test the API.')


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, ET.ParseError) as error:
        print(f'Planning check failed: {error}', file=sys.stderr)
        sys.exit(1)
