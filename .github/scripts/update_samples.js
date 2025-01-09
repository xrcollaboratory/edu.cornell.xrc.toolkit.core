const fs = require("fs");
const path = require("path");

// Read and parse package.json
const packageJson = JSON.parse(fs.readFileSync("package.json", "utf8"));

if (!packageJson.samples || !Array.isArray(packageJson.samples)) {
    console.log("No samples array found in package.json");
    process.exit(0);
}

// Helper function to find all images in a sample's folder
function findSampleImages(sampleName) {
    const sampleImagesDir = path.join("Documentation~", "images", "samples", sampleName);
    const images = [];

    // Check if directory exists
    if (!fs.existsSync(sampleImagesDir)) {
        return images;
    }

    try {
        // Read all files in the directory
        const files = fs.readdirSync(sampleImagesDir);

        // Filter image files
        const imageFiles = files.filter(file => {
            const ext = path.extname(file).toLowerCase();
            return ['.gif', '.png', '.jpg', '.jpeg'].includes(ext);
        });

        // Sort alphabetically
        imageFiles.sort();

        // Create relative image paths with URL-encoded file and folder names
        images.push(...imageFiles.map(file => {
            return `../images/samples/${encodeURIComponent(sampleName)}/${encodeURIComponent(file)}`;
        }));
    } catch (error) {
        console.error(`Error reading directory for ${sampleName}:`, error);
    }

    return images;
}

// Helper function to format a sample object into markdown
function formatSample(sample) {
    const { displayName, description } = sample;
    const lines = [
        `## ${displayName}`,
        "",
        description,
        ""
    ];

    // Add all images found in the sample's folder after the description
    const imagePaths = findSampleImages(displayName);
    for (const imagePath of imagePaths) {
        lines.push(`![${displayName}](${imagePath})`);
        lines.push("");
    }

    return lines.join("\n");
}

// Create directory if it doesn't exist
const docDir = path.join("Documentation~", "manual");
fs.mkdirSync(docDir, { recursive: true });

// Generate markdown content
const markdown = [
    "# Samples",
    "",
    "This document lists all available samples in the package.",
    "",
    ...packageJson.samples.map(formatSample),
    "",
    "_This document is automatically generated. Do not edit manually._"
].join("\n");

// Write to file
fs.writeFileSync(
    path.join(docDir, "samples.md"),
    markdown
);