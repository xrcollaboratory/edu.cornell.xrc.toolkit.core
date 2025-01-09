const fs = require("fs");
const path = require("path");

// Read and parse package.json
const packageJson = JSON.parse(fs.readFileSync("package.json", "utf8"));

// Extract author name from package.json
function getAuthorName(author) {
    if (typeof author === 'string') {
        // If author is just a string, return it
        return author;
    } else if (typeof author === 'object' && author !== null) {
        // If author is an object, return the name property
        return author.name || '';
    }
    return '';
}

const authorName = getAuthorName(packageJson.author);

// Path to docfx.json
const docfxPath = path.join("Documentation~", "docfx.json");

// Check if docfx.json exists
if (!fs.existsSync(docfxPath)) {
    console.error("docfx.json not found at:", docfxPath);
    process.exit(1);
}

// Read docfx.json as string to preserve formatting
const docfxContent = fs.readFileSync(docfxPath, "utf8");

// Parse docfx.json
const docfxJson = JSON.parse(docfxContent);

// Update only the specific values while preserving the rest
if (docfxJson.build && docfxJson.build.globalMetadata) {
    docfxJson.build.globalMetadata._appTitle = packageJson.displayName;
    docfxJson.build.globalMetadata._appFooter = authorName;
}

if (docfxJson.build && docfxJson.build.sitemap) {
    docfxJson.build.sitemap.baseUrl = packageJson.documentationUrl;
}

// Write updated docfx.json while preserving formatting
fs.writeFileSync(
    docfxPath,
    JSON.stringify(docfxJson, null, 4)
);