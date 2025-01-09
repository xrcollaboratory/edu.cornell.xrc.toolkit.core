const fs = require("fs");
const path = require("path");

function convertXmlToMarkdown(text) {
    if (!text) return text;

    // Handle <see cref="..."/> tags
    text = text.replace(/<see cref="([^"]+)"\/>/g, '`$1`');
    text = text.replace(/<see cref="([^"]+)">\s*([^<]+)<\/see>/g, '`$2`');

    // Handle <c>...</c> tags (inline code)
    text = text.replace(/<c>([^<]+)<\/c>/g, '`$1`');

    // Handle <code>...</code> blocks
    text = text.replace(/<code>([^<]+)<\/code>/g, '```\n$1\n```');

    // Handle <paramref name="..."/> tags
    text = text.replace(/<paramref name="([^"]+)"\/>/g, '`$1`');

    // Handle <typeparamref name="..."/> tags
    text = text.replace(/<typeparamref name="([^"]+)"\/>/g, '`$1`');

    // Handle <b>...</b> tags
    text = text.replace(/<b>([^<]+)<\/b>/g, '**$1**');

    // Handle <i>...</i> tags
    text = text.replace(/<i>([^<]+)<\/i>/g, '*$1*');

    // Handle <list type="bullet|number"> tags
    text = text.replace(/<list type="bullet">\s*([^<]*)<\/list>/g, (match, items) => {
        return items.split('<item>').slice(1).map(item =>
            `* ${item.replace('</item>', '').trim()}`
        ).join('\n');
    });

    text = text.replace(/<list type="number">\s*([^<]*)<\/list>/g, (match, items) => {
        return items.split('<item>').slice(1).map((item, index) =>
            `${index + 1}. ${item.replace('</item>', '').trim()}`
        ).join('\n');
    });

    return text;
}

function findComponentImage(className) {
    const imageDir = path.join("Documentation~", "images", "components");
    const extensions = ['.png', '.jpg', '.jpeg'];

    for (const ext of extensions) {
        const imagePath = path.join(imageDir, className + ext);
        if (fs.existsSync(imagePath)) {
            return `../images/components/${className}${ext}`;
        }
    }

    return null;
}

function formatName(name) {
    // Split by capital letters and numbers
    let formatted = name.replace(/([A-Z][a-z]+|[0-9]+)/g, ' $1').trim();

    // Handle consecutive capital letters (like UI, AI, etc.)
    formatted = formatted.replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2');

    // Remove extra spaces and ensure first letter is capitalized
    formatted = formatted.replace(/\s+/g, ' ').trim();
    formatted = formatted.charAt(0).toUpperCase() + formatted.slice(1);

    return formatted;
}

function formatFieldName(fieldName) {
    // Remove m_ prefix if it exists
    let formatted = fieldName.replace(/^m_/, '');
    return formatName(formatted);
}

function findPropertySummary(content, propertyName) {
    console.log(`Looking for property summary: ${propertyName}`);

    // First find the property declaration
    const propertyPattern = new RegExp(`public\\s+[^\\n]+?\\s+${propertyName}\\s*{[^}]*}`);
    const propertyMatch = propertyPattern.exec(content);

    if (propertyMatch) {
        // Get the content before this property
        const beforeProperty = content.substring(0, propertyMatch.index);

        // Find the closest summary before the property
        const lastIndex = beforeProperty.lastIndexOf('/// <summary>');
        if (lastIndex !== -1) {
            const summaryEndIndex = beforeProperty.indexOf('/// </summary>', lastIndex);
            if (summaryEndIndex !== -1) {
                const summaryContent = beforeProperty.substring(lastIndex + 12, summaryEndIndex);
                const summary = summaryContent
                    .split('\n')
                    .map(line => line.replace(/^\s*\/\/\/\s*/, ''))
                    .join(' ')
                    .trim()
                    .replace(/^>\s*/, ''); // Remove leading '>' character
                console.log(`Found summary for ${propertyName}:`, summary);
                return convertXmlToMarkdown(summary);
            }
        }
    }

    console.log(`No summary found for ${propertyName}`);
    return null;
}

function findSerializedFields(content, classStartIndex) {
    // Find the class content
    let braceCount = 1;
    let classEndIndex = classStartIndex;

    // Find the opening brace of the class
    for (let i = classStartIndex; i < content.length; i++) {
        if (content[i] === '{') {
            classEndIndex = i + 1;
            break;
        }
    }

    // Find the closing brace of the class
    while (braceCount > 0 && classEndIndex < content.length) {
        if (content[classEndIndex] === '{') braceCount++;
        if (content[classEndIndex] === '}') braceCount--;
        classEndIndex++;
    }

    const classContent = content.substring(classStartIndex, classEndIndex);

    // Find all serialized fields
    const fields = [];

    // First find all [SerializeField] attributes
    const serializeFieldLocations = [...classContent.matchAll(/\[SerializeField\]/g)].map(match => match.index);

    for (const location of serializeFieldLocations) {
        // Look for the next private field declaration after this attribute
        const afterAttribute = classContent.substring(location);
        const fieldMatch = afterAttribute.match(/(?:(?:\[[^\]]+\][\s\n]*)*)?private\s+([^\n;]+?)\s+(\w+)\s*(?:=\s*[^;]+)?;/);

        if (fieldMatch) {
            const fieldType = fieldMatch[1].trim();
            const fieldName = fieldMatch[2];
            console.log(`\nProcessing field: ${fieldName}`);

            // Convert field name to property name (remove m_ prefix)
            const propertyBaseName = fieldName.replace(/^m_/, '');

            // Try both pascal case and camel case for the property name
            const pascalCaseName = propertyBaseName.charAt(0).toUpperCase() + propertyBaseName.slice(1);
            const camelCaseName = propertyBaseName.charAt(0).toLowerCase() + propertyBaseName.slice(1);

            console.log(`Looking for property names: ${pascalCaseName} or ${camelCaseName}`);

            // Try to find summary for either name
            const summary = findPropertySummary(classContent, pascalCaseName) ||
                findPropertySummary(classContent, camelCaseName);

            fields.push({
                name: fieldName,
                displayName: formatFieldName(fieldName),
                type: fieldType,
                summary: summary || '*No description provided*'
            });
        }
    }

    return fields;
}

function scanFile(filePath) {
    console.log('\n=== Processing file:', filePath, '===');
    const content = fs.readFileSync(filePath, 'utf8');
    const components = [];

    // Find classes
    const classRegex = /public\s+class\s+(\w+)\s*:\s*MonoBehaviour/g;
    let classMatch;

    while ((classMatch = classRegex.exec(content)) !== null) {
        const className = classMatch[1];
        const displayName = formatName(className);
        console.log('\nFound class:', className);

        // Find summary
        const beforeClass = content.substring(0, classMatch.index);
        const summaryRegex = /\/\/\/\s*<summary>([^]*?)\/\/\/\s*<\/summary>/;
        const summaryMatch = beforeClass.match(summaryRegex);

        let summary = '';
        if (summaryMatch) {
            summary = summaryMatch[1]
                .split('\n')
                .map(line => line.replace(/^\s*\/\/\/\s*/, ''))
                .join(' ')
                .trim();
            summary = convertXmlToMarkdown(summary);
            console.log('Found class summary:', summary);
        }

        // Find serialized fields and their summaries
        const fields = findSerializedFields(content, classMatch.index);
        console.log(`\nFound ${fields.length} serialized fields in ${className}:`);
        fields.forEach(field => {
            console.log(` - ${field.displayName}: ${field.summary}`);
        });

        // Find component image if it exists
        const imagePath = findComponentImage(className);
        console.log(imagePath ? `Found image for ${className}: ${imagePath}` : `No image found for ${className}`);

        components.push({
            className,
            displayName,
            path: filePath,
            summary,
            fields,
            imagePath
        });
    }

    return components;
}

function scanDirectory(dir) {
    console.log('\nScanning directory:', dir);
    const components = [];

    function walkDir(currentPath) {
        const files = fs.readdirSync(currentPath);
        for (const file of files) {
            const filePath = path.join(currentPath, file);
            const stat = fs.statSync(filePath);

            if (stat.isDirectory()) {
                walkDir(filePath);
            } else if (file.endsWith('.cs')) {
                const fileComponents = scanFile(filePath);
                components.push(...fileComponents);
            }
        }
    }

    walkDir(dir);
    console.log(`\nTotal components found: ${components.length}`);
    return components;
}

// Main execution
console.log('\nStarting component documentation generation...');

// Scan Runtime folder
const runtimeDir = path.join(process.cwd(), 'Runtime');
if (!fs.existsSync(runtimeDir)) {
    console.error('Runtime directory not found');
    process.exit(1);
}

// Get components
const components = scanDirectory(runtimeDir);

// Generate markdown content
console.log('\nGenerating markdown...');
const markdown = [
    "# Components",
    "",
    "This document lists all MonoBehaviour components in the package.",
    "",
];

for (const component of components) {
    markdown.push(
        `## ${component.displayName}`,
        "",
        component.summary || '*No summary provided*',
        ""
        // `Source: \`${path.relative(process.cwd(), component.path)}\``,
        //""
    );

    if (component.imagePath) {
        markdown.push(
            `![${component.displayName}](${component.imagePath})`,
            ""
        );
    }

    if (component.fields.length > 0) {
        markdown.push(
            "| Property | Description |",
            "|----------|-------------|",
            ...component.fields.map(field =>
                `| **${field.displayName}** | ${field.summary} |`
            ),
            ""
        );
    }
}

markdown.push("_This file is automatically generated. Do not edit manually._");

const docDir = path.join("Documentation~", "manual");
fs.mkdirSync(docDir, { recursive: true });
const outputPath = path.join(docDir, "components.md");
fs.writeFileSync(outputPath, markdown.join("\n"));