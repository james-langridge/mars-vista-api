# Git Commit Guide for Claude Code

This guide synthesizes best practices for making commits and writing commit messages, based on insights from GitHub Blog, Tim Pope, Dan Thompson, Joel Chippindale, and Tekin Süleyman.

## ⚠️ CRITICAL RULES FOR AI ASSISTANTS

**ABSOLUTELY FORBIDDEN in commit messages:**
1. ❌ ANY attribution to AI/Claude (no "Generated with Claude Code")
2. ❌ Co-authorship lines (no "Co-Authored-By: Claude")
3. ❌ Emoji or special characters
4. ❌ References to internal docs (/claude/ directory, CLAUDE.md)

**REQUIRED commit format:**
```
Imperative summary (50 chars max)

- Technical change 1
- Technical change 2
```

## Core Principles

### 1. Make Atomic Commits
**Every commit should represent one logical, self-contained change.**

- A commit should do one thing and do it completely
- **The repository should build and pass tests at every commit** - run `npm run build` before committing
- Avoid commits that say "X and Y" - these should likely be two commits
- Think in terms of "minimum viable commits" - the smallest useful change

**Size Guidelines:**
- **Ideal commit size**: 5-50 lines of code (excluding generated files)
- **Maximum commit size**: 100-200 lines for complex changes
- **File count**: Prefer 1-3 files per commit (unless they're tightly coupled)
- **Time to implement**: If it takes more than 30 minutes, it's probably multiple commits

**Red Flags Your Commit is Too Large:**
- The commit message uses "and" multiple times
- You're creating multiple new components/features
- You're touching more than one subsystem
- The diff is hard to review in one sitting
- You're fixing bugs while adding features

**When to Commit:**
- After creating a new component (even if incomplete)
- After implementing one method/function
- After fixing one bug
- After adding one test
- After refactoring one module
- After updating documentation for one feature

**Benefits:**
- Makes code review manageable
- Enables effective use of `git bisect` for finding bugs
- Allows selective reverting of changes
- Creates a clear historical record

### 2. Structure Your Narrative
**Your commit history should tell a coherent story.**

- Organize commits logically, not chronologically
- Group related changes together
- Put refactoring commits immediately before the features they enable
- Keep different types of changes separate (bugfix, refactor, feature, style)
- One high-level concept per branch

**Anti-patterns to avoid:**
- Jumping between unrelated topics
- Mixing bug fixes with feature development
- Including debugging/experimental commits in final history
- Commits that undo previous commits in the same branch

### 3. Write Meaningful Commit Messages

#### Format

```
Capitalized, short (50 chars or less) summary

More detailed explanatory text, if necessary. Wrap it to about 72
characters or so. In some contexts, the first line is treated as the
subject of an email and the rest of the text as the body. The blank
line separating the summary from the body is critical (unless you omit
the body entirely).

Explain the context and why you're making the change, not just what
the change is. The future reader (likely you!) will thank you.

Further paragraphs come after blank lines.

- Bullet points are okay, too
- Typically a hyphen or asterisk is used for the bullet
- Use a hanging indent
```

**Why these formatting rules matter:**
- **50-character subject**: Prevents truncation in UI tools and logs
- **72-character body**: Readable in 80-column terminals with room for indentation
- **Blank line after subject**: Tools expect this separation
- **Imperative mood**: Matches Git's own convention (merge, revert commits)

#### Content Guidelines

**Subject Line (First Line):**
- Use imperative mood: "Fix bug" not "Fixed bug" or "Fixes bug"
- Be specific but concise
- Don't end with a period
- Must pass the **Imperative Mood Test**: "If applied, this commit will [your subject line]"
  - ✅ "If applied, this commit will **fix the null pointer exception**"
  - ❌ "If applied, this commit will **fixed the null pointer exception**"

**Optional Conventional Commit Prefixes:**
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation only
- `style:` - Formatting, missing semicolons, etc.
- `refactor:` - Code change that neither fixes a bug nor adds a feature
- `test:` - Adding missing tests
- `chore:` - Maintenance tasks

**What NOT to Include:**
- Do not reference internal documentation files (e.g., files in ./claude/)
- Do not include AI/Claude authorship attribution
- Do not add emoji or special formatting beyond standard markdown
- Keep messages focused on the technical changes, not the process

**Body Should Answer:**
1. **What** does this accomplish? (high-level intent)
2. **How** did you accomplish it? (implementation approach)
3. **Why** make this change? (context and motivation)
4. **Why** this approach? (alternatives considered)

#### Example of an Excellent Commit Message

```
Fix rendering of FAQ link in email footer

In some email clients the colour of the FAQ link in the course notice
footer was being displayed as blue instead of white. 

The issue only occurred in various versions of Outlook, which won't 
implement CSS changes that include `!important` inline[1]. Since we 
were using `!important` to define the link colour, Outlook ignored 
the style and applied its default (blue).

Removing `!important` from the inline style fixes the rendering while
maintaining the intended white colour in all tested clients.

[1] https://www.campaignmonitor.com/blog/post/3143/
```

#### Good vs Bad Examples

❌ **Bad:**
```
Fixed stuff
updated code  
bugfix
changes
Implement architecture visualization with React Flow
Add authentication system and user management
```

✅ **Good:**
```
Fix null pointer exception in user service

Add password strength validation to signup

Refactor database connection handling

The previous implementation created a new connection for each query,
leading to connection pool exhaustion under load. This commit 
introduces a connection manager that reuses connections and properly
handles cleanup.

Performance tests show a 3x improvement in throughput and elimination
of connection timeout errors that were occurring every 2-3 hours in
production.
```

#### Example: Breaking Down a Feature Implementation

❌ **Bad - Single Large Commit:**
```
Implement user authentication system

- Add login/logout endpoints
- Create user model and database schema  
- Add password hashing
- Implement JWT tokens
- Add auth middleware
- Create login UI components
- Add user profile page
- Write tests
```

✅ **Good - Series of Atomic Commits:**
```
1. Add User model with basic fields
2. Add password hashing utilities
3. Create user registration endpoint
4. Add login endpoint with JWT generation
5. Add auth middleware for protected routes
6. Create LoginForm component
7. Add logout endpoint
8. Create UserProfile component
9. Add user service tests
10. Add integration tests for auth flow
```

## When to Make Commits

### During Development
- Commit when you complete one logical change
- Commit before switching context to another task
- Commit when tests pass after implementing new functionality
- Commit before attempting a risky refactoring (so you can easily revert)
- **CRITICAL: Always run `npm run build` before committing - the build MUST pass without errors**
- **NEVER use `git add -f` or `--force` to override .gitignore patterns**
- Always check `git status` to ensure you're not adding ignored files
- Files in `/claude` directory and `CLAUDE.md` are for AI guidance only and must never be committed

### Before Sharing
- Use `git rebase -i` to clean up your development history
- Squash fix-up commits into the commits they fix
- Reorder commits to tell a logical story
- Split commits that do too much
- Remove debugging or experimental commits

## Practical Benefits

### For Code Review
- Reviewers can go through commits one at a time
- Each commit provides focused context
- Large PRs become manageable
- Issues are easier to spot in isolated changes

### For Debugging
- `git bisect` can efficiently find bug origins
- `git blame` provides meaningful context for each line
- `git log --grep` helps find relevant changes
- Error messages in commits are searchable

### For Documentation
- Every line of code has documented history
- Intent and context are preserved
- Alternative approaches are recorded
- The "why" survives personnel changes

### Where Your Subject Line Appears
Understanding why the 50-character limit matters:
- `git log --oneline` - Shows only the subject
- `git rebase -i` - Lists commits by subject  
- GitHub/GitLab UI - Shows subject prominently
- `git shortlog` - Groups commits by author and subject
- Email tools - Uses as email subject line
- `git reflog` - Local history browser
- Merge commit messages - When `merge.summary` is set
- Various GUI tools (gitk, SourceTree, etc.)

## Making This Practical

### For Individual Developers
- These practices make your work easier immediately:
  - Clear commits help you verify your changes work correctly
  - Good messages help you remember what you did and why
  - Clean history makes PR reviews faster and more pleasant

### For Teams
- Establish shared understanding of commit quality
- Use PR reviews to reinforce good practices
- Share examples of excellent commits
- Remember: commit messages are for humans, not machines

## Git Commands for Clean History

```bash
# Interactive rebase to clean up last n commits
git rebase -i HEAD~n

# Amend the last commit
git commit --amend

# Add changes to previous commit without changing message
git commit --amend --no-edit

# Find commits touching specific text
git log -S "search term"

# Search commit messages
git log --grep="pattern"

# Show who last modified each line
git blame filename

# Find which commit introduced a bug
git bisect start
git bisect bad
git bisect good <known-good-commit>

# Check what files will be added BEFORE committing
git status

# See what's in .gitignore
cat .gitignore

# Check if a file is ignored
git check-ignore -v path/to/file
```

## Editor Configuration

### Vim
```vim
:set textwidth=72
```
Or install vim-git runtime files for automatic formatting.

### VS Code
- Install the "Rewrap" extension
- Set wrap column to 72 in settings

### TextMate
```bash
defaults write com.macromates.textmate OakWrapColumns '( 40, 72, 78 )'
```

## Key Takeaways

1. **Every commit should be atomic and stable** - one change, fully complete
2. **Commit messages should explain intent** - focus on why, not just what
3. **History should tell a story** - organize commits for clarity
4. **Clean up before sharing** - use rebase to perfect your history
5. **Think of future readers** - including yourself in six months

Remember: "The primary goal of a software developer should be to communicate their intent to future developers" - Louise Crow

Your commit history is not just a log - it's the story of your software's evolution. Make it a good story.

