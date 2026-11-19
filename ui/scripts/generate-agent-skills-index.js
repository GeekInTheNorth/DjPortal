// Writes dist/.well-known/agent-skills/index.json per the Agent Skills Discovery RFC v0.2.0
// (https://github.com/cloudflare/agent-skills-discovery-rfc).
//
// Each skill lives in public/.well-known/agent-skills/<name>/SKILL.md and Vite copies it into dist.
// The index carries a SHA-256 digest of each SKILL.md's exact bytes, and clients reject a skill whose
// digest doesn't match. Generating the index from the built files keeps the two in step - never edit
// a SKILL.md in dist or hand-write the index.

import crypto from 'crypto';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SKILLS_DIR = path.resolve(__dirname, '..', 'dist', '.well-known', 'agent-skills');
const SKILLS_URL_PATH = '/.well-known/agent-skills';
const SCHEMA_URL = 'https://schemas.agentskills.io/discovery/0.2.0/schema.json';
const NAME_PATTERN = /^[a-z0-9]+(-[a-z0-9]+)*$/;

function readFrontmatter(markdown, filePath) {
    const match = markdown.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n/);
    if (!match) {
        throw new Error(`${filePath} has no YAML frontmatter.`);
    }

    // The frontmatter only holds simple "key: value" lines, so a full YAML parser isn't needed.
    const fields = {};
    for (const line of match[1].split(/\r?\n/)) {
        const separator = line.indexOf(':');
        if (separator > 0) {
            fields[line.slice(0, separator).trim()] = line.slice(separator + 1).trim();
        }
    }

    return fields;
}

function buildSkillEntry(directoryName) {
    const skillPath = path.join(SKILLS_DIR, directoryName, 'SKILL.md');
    if (!fs.existsSync(skillPath)) {
        return null;
    }

    const bytes = fs.readFileSync(skillPath);
    const { name, description } = readFrontmatter(bytes.toString('utf-8'), skillPath);

    if (name !== directoryName || !NAME_PATTERN.test(name)) {
        throw new Error(`${skillPath}: frontmatter name "${name}" must match its folder name and be lowercase letters, digits and hyphens.`);
    }
    if (!description || description.length > 1024) {
        throw new Error(`${skillPath}: frontmatter description is missing or longer than 1024 characters.`);
    }

    return {
        name,
        type: 'skill-md',
        description,
        url: `${SKILLS_URL_PATH}/${name}/SKILL.md`,
        digest: `sha256:${crypto.createHash('sha256').update(bytes).digest('hex')}`
    };
}

function main() {
    if (!fs.existsSync(SKILLS_DIR)) {
        console.error(`Error: ${SKILLS_DIR} not found. Run "vite build" first.`);
        process.exit(1);
    }

    const skills = fs.readdirSync(SKILLS_DIR, { withFileTypes: true })
        .filter(entry => entry.isDirectory())
        .map(entry => buildSkillEntry(entry.name))
        .filter(Boolean)
        .sort((a, b) => a.name.localeCompare(b.name));

    const index = {
        $schema: SCHEMA_URL,
        skills
    };

    fs.writeFileSync(path.join(SKILLS_DIR, 'index.json'), `${JSON.stringify(index, null, 2)}\n`, 'utf-8');
    console.log(`Generated agent-skills index.json with ${skills.length} skill(s).`);
}

try {
    main();
} catch (error) {
    console.error(`Error generating agent-skills index: ${error.message}`);
    process.exit(1);
}
