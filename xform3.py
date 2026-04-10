import re, glob

UNIT_TEST_PATH = "D:/Workspace/gitProject/StaticDataPipeline/UnitTest"

def parse_raw_string(text, start):
    """Find end of raw """...""" block starting right after the opening """.
    Returns (content, end_position)."""
    i = start
    content_chars = []
    while i < len(text):
        if text[i] == '"' and text[i:i+3] == '"""':
            return ''.join(content_chars), i + 3
        content_chars.append(text[i])
        i += 1
    return None, -1

def fix_interpolation_in_dollar_raw(content):
    """In $"""...""" content, convert {identifier} -> {{identifier}}."""
    return re.sub(r'\{([A-Za-z_][A-Za-z0-9_]*)\}', r'{{\1}}', content)

def fix_single_brace_in_dollar_dollar_raw(content):
    """In $$"""...""" content, convert {identifier} -> {{identifier}} when it looks like an interpolation hole."""
    return re.sub(r'\{([A-Za-z_][A-Za-z0-9_]*)\}', r'{{\1}}', content)

def fix_indentation(var_indent, prefix, content):
    """Fix indentation of raw string content and closing delimiter.
    var_indent: indent of 'var code = ' line (number of spaces)
    prefix: "" or "$$"
    content: raw string content including closing """
    Returns fixed content."""
    expected = var_indent + 11 + len(prefix)

    lines = content.split('\n')

    # Find the closing """ line
    close_idx = None
    for i in range(len(lines) - 1, -1, -1):
        stripped = lines[i].strip()
        if stripped == '"""':
            close_idx = i
            break

    if close_idx is None:
        return content

    # Determine current closing """ indent
    current_close = len(lines[close_idx]) - len(lines[close_idx].lstrip())

    if current_close == expected:
        return content

    delta = expected - current_close

    # Re-indent all non-empty content lines and closing line
    new_lines = []
    for i, line in enumerate(lines):
        if i == 0:
            # First line is empty (right after opening """)
            new_lines.append(line)
        elif i == close_idx:
            # Closing """
            new_lines.append(' ' * expected + '"""')
        elif line.strip() == '':
            new_lines.append('')
        else:
            curr_indent = len(line) - len(line.lstrip())
            new_indent = max(0, curr_indent + delta)
            new_lines.append(' ' * new_indent + line.lstrip())

    return '\n'.join(new_lines)

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8', newline='') as f:
        text = f.read()

    if 'var code = ' not in text:
        return False

    original = text
    result_parts = []
    i = 0

    # Pattern: var code = [optional $$ or $]"""
    var_pattern = re.compile(r'(\n( +)// language=C#\n\2var code = )(\$\$|\$|)(""")')

    while True:
        m = var_pattern.search(text, i)
        if not m:
            result_parts.append(text[i:])
            break

        result_parts.append(text[i:m.start()])

        prefix_before = m.group(1)   # \n + indent + // language=C# + \n + indent + var code =
        var_indent_str = m.group(2)  # indent string
        var_indent = len(var_indent_str)
        dollar_prefix = m.group(3)   # $$, $, or empty
        open_delim = m.group(4)      # """

        after_open = m.end()
        content, end_pos = parse_raw_string(text, after_open)

        if content is None:
            result_parts.append(text[m.start():])
            break

        # Determine new prefix and fix content
        if dollar_prefix == '$':
            # Revert $""" -> $$""" and {x} -> {{x}}
            new_prefix = '$$'
            content = fix_interpolation_in_dollar_raw(content)
        elif dollar_prefix == '$$':
            # Fix {identifier} -> {{identifier}} in $$""" (for unused params)
            new_prefix = '$$'
            content = fix_single_brace_in_dollar_dollar_raw(content)
        else:
            # Plain """
            new_prefix = ''

        # Fix indentation
        content = fix_indentation(var_indent, new_prefix, content)

        replacement = prefix_before + new_prefix + '"""' + content + '"""'
        result_parts.append(replacement)
        i = end_pos

    text = ''.join(result_parts)

    if text != original:
        with open(filepath, 'w', encoding='utf-8', newline='') as f:
            f.write(text)
        return True
    return False

changed = []
for fp in sorted(glob.glob(UNIT_TEST_PATH + '/**/*.cs', recursive=True)):
    if process_file(fp):
        changed.append(fp)

print("Changed %d files:" % len(changed))
for fp in changed:
    print("  " + fp.replace(UNIT_TEST_PATH, ''))
