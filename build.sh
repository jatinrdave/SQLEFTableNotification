#!/bin/bash

echo "Building SQLDBEntityNotifier solution..."

# Clean and restore
echo "Cleaning and restoring packages..."
dotnet clean
dotnet restore

# Build the solution
echo "Building solution..."
dotnet build --configuration Release --no-restore

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    
    # Run unit tests
    echo "Running unit tests..."
    dotnet test --configuration Release --no-build --verbosity normal
    
    if [ $? -eq 0 ]; then
        echo "✅ All tests passed!"
    else
        echo "❌ Some tests failed!"
        exit 1
    fi
else
    echo "❌ Build failed!"
    exit 1
fi

echo "🎉 Build and test completed successfully!"